namespace Disharmony;

public partial class InstructionMatcher
{
    private class Worker(InstructionMatcher instructionMatcher, MethodBase method, List<CodeInstruction> inInstructions, ILGenerator generator, bool debug)
    {
        private readonly Dictionary<int, int> localMap_Method = new();
        private readonly Dictionary<Label, Label> labelMap_Method = new();
        private readonly List<MatchData> matches = [];
        private readonly List<ExceptionBlock> extraBlocks = [];

        public readonly List<CodeInstruction> OutInstructions = [];

        public void MatchAndReplace()
        {
            if (debug || forceDebug)
                FileLog.Log($"## InfixPatcher {method.FullName}");

            // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
            for (var ruleIndex = 0; ruleIndex < instructionMatcher.Rules.Count; ruleIndex++)
            {
                Rule rule = instructionMatcher.Rules[ruleIndex];

                switch (rule)
                {
                    case { Mode: OutputMode.MatchOnly, Output: not null }:
                        throw new InvalidOperationException($"{rule.Mode} rule cannot have Output = null");
                    case { Mode: not OutputMode.MatchOnly, Output: null }:
                        throw new InvalidOperationException($"{rule.Mode} rule must have Output = null");
                    case { Mode: OutputMode.MethodPrefix or OutputMode.MethodPostfix, Pattern.Length: > 0 }:
                        throw new InvalidOperationException($"{rule.Mode} cannot have a Pattern");
                    case { Mode: not (OutputMode.MethodPrefix or OutputMode.MethodPostfix), Pattern: not { Length: > 0 } }:
                        throw new InvalidOperationException($"{rule.Mode} rule must have a Pattern");
                }

                switch (rule.Mode)
                {
                    case OutputMode.MethodPrefix:
                    {
                        matches.Add(new MatchData
                        {
                            rule = rule,
                            start = 0,
                            end = 0,
                            localMap_Match = new(),
                            labelMap_Match = new(),
                        });
                        continue;
                    }
                    case OutputMode.MethodPostfix:
                    {
                        matches.Add(new MatchData
                        {
                            rule = rule,
                            start = inInstructions.Count,
                            end = inInstructions.Count,
                            localMap_Match = new(),
                            labelMap_Match = new(),
                        });
                        continue;
                    }
                }

                var matchCount = 0;

                for (int instructionIndex = rule.Chained && matches.Count > 0 ? matches[^1].end : 0;
                     instructionIndex <= inInstructions.Count - rule.Pattern.Length;
                     instructionIndex++)
                {
                    var isMatch = true;
                    var localIndex_Match = new Dictionary<int, int>();

                    for (var patternIndex = 0; patternIndex < rule.Pattern.Length; patternIndex++)
                    {
                        CodeInstruction inst = inInstructions[instructionIndex + patternIndex];
                        CodeInstruction patternInst = rule.Pattern[patternIndex];

                        // For a load or store, map the local indexes in the pattern to the actual local indexes used
                        // in the function
                        if (patternInst.IsStloc())
                        {
                            isMatch = inst.IsStloc();
                            if (!isMatch)
                                break;

                            int localIndex = patternInst.LocalIndex();
                            int targetIndex = inst.LocalIndex();

                            if (localMap_Method.TryGetValue(localIndex, out int substituteIndex) ||
                                localIndex_Match.TryGetValue(localIndex, out substituteIndex))
                                isMatch = targetIndex == substituteIndex;
                            else
                                localIndex_Match.Add(localIndex, targetIndex);
                        }
                        else if (patternInst.opcode.Value == OpCodes.Ldloca.Value ||
                                 patternInst.opcode.Value == OpCodes.Ldloca_S.Value)
                        {
                            isMatch = inst.opcode == patternInst.opcode;
                            if (!isMatch)
                                break;

                            throw new NotSupportedException();
                        }
                        else if (patternInst.IsLdloc())
                        {
                            isMatch = inst.IsLdloc() &&
                                      inst.opcode.Value != OpCodes.Ldloca.Value &&
                                      inst.opcode.Value != OpCodes.Ldloca_S.Value;
                            if (!isMatch)
                                break;

                            int localIndex = patternInst.LocalIndex();

                            // There is something very weird going on here. This may be a Harmony bug.
                            int targetIndex = inst.operand is LocalBuilder lb ? lb.LocalIndex : inst.LocalIndex();

                            if (localMap_Method.TryGetValue(localIndex, out int substituteIndex) ||
                                localIndex_Match.TryGetValue(localIndex, out substituteIndex))
                                isMatch = targetIndex == substituteIndex;
                            else
                                localIndex_Match.Add(localIndex, targetIndex);
                        }
                        // For convenience, let call also match callvirt. Nobody wants to worry about
                        // the difference when writing patterns.
                        else if (patternInst.opcode.Value == OpCodes.Call.Value)
                        {
                            isMatch = (inst.opcode.Value == OpCodes.Call.Value ||
                                       inst.opcode.Value == OpCodes.Callvirt.Value) &&
                                      inst.operand.Equals(patternInst.operand);
                        }
                        else if (patternInst.operand == null)
                        {
                            isMatch = inst.opcode.Value == patternInst.opcode.Value && inst.operand == null;
                        }
                        else
                            isMatch = inst.Is(patternInst.opcode, patternInst.operand);

                        if (!isMatch)
                            break;
                    }

                    if (!isMatch)
                        continue;

                    var matchData = new MatchData
                    {
                        rule = rule,
                        start = instructionIndex,
                        end = instructionIndex + rule.Pattern.Length,
                        localMap_Match = localIndex_Match,
                        labelMap_Match = new(),
                    };
                    if (debug || forceDebug)
                        FileLog.Log($"MATCH #{ruleIndex} ({matchData.start} .. {matchData.end - 1})");

                    if (rule.Output != null)
                        matches.Add(matchData);

                    if (rule.SaveLocals)
                    {
                        foreach (var kvp in localIndex_Match)
                            localMap_Method.Add(kvp.Key, kvp.Value);
                    }

                    matchCount++;
                    if (rule.Max > 0 && matchCount >= rule.Max)
                        break;
                }

                if (matchCount < rule.Min)
                {
                    throw new InvalidOperationException($"Not enough matches found for substitution #{ruleIndex}");
                }
            }

            var sortedMatches = matches.OrderBy(m => m.start).ToList();
            for (var i = 0; i < sortedMatches.Count - 1; i++)
            {
                if (sortedMatches[i].end > sortedMatches[i + 1].start)
                {
                    throw new InvalidOperationException("Overlapping matches");
                }
            }

            if (matches.Count == 0)
            {
                throw new InvalidOperationException("No matches");
            }

            // Make the substitutions
            for (var instructionIndex = 0;; instructionIndex++)
            {
                int index = instructionIndex;
                var match = sortedMatches.FirstOrDefault(r => r.start == index && !r.emitted);

                if (match == null)
                {
                    if (instructionIndex >= inInstructions.Count)
                        break;

                    Emit(inInstructions[instructionIndex]);
                    if (debug || forceDebug)
                        FileLog.Log($"COPY {OutInstructions[^1]}");

                    continue;
                }

                match.emitted = true;

                if (match.rule.Mode == OutputMode.InsertAfter)
                {
                    for (int i = match.start; i < match.end; i++)
                    {
                        Emit(inInstructions[i]);
                        if (debug || forceDebug)
                            FileLog.Log($"COPY MATCH {OutInstructions[^1]}");
                    }
                }

                instructionIndex = match.end - 1;

                if (match.rule.Mode is OutputMode.Replace or OutputMode.InsertBefore &&
                    inInstructions[match.start] is { labels: { Count: > 0 } labels })
                {
                    Emit(new(OpCodes.Nop) { labels = labels });
                }

                Emit(CodeInstruction.Annotation($"Begin {match.rule.Name}"));

                extraBlocks.AddRange(inInstructions[match.start].blocks);

                foreach (CodeInstruction replaceInst in match.rule.Output)
                {
                    EmitReplacement(replaceInst, match);

                    if (debug || forceDebug)
                        FileLog.Log($"EMIT {OutInstructions[^1]}");
                }

                extraBlocks.Clear();

                Emit(CodeInstruction.Annotation($"End {match.rule.Name}"));

                if (match.rule.Mode == OutputMode.InsertBefore)
                {
                    for (int i = match.start; i < match.end; i++)
                    {
                        Emit(inInstructions[i]);

                        if (i == match.start)
                            OutInstructions[^1].labels.Clear();

                        if (debug || forceDebug)
                            FileLog.Log($"COPY MATCH {OutInstructions[^1]}");
                    }
                }
            }

            if (debug || forceDebug)
                FileLog.Log("");
        }

        private void EmitReplacement(CodeInstruction replaceInst, MatchData match)
        {
            if (replaceInst.IsStloc())
            {
                var substituteIndex = GetReplacementLocal(replaceInst.LocalIndex(), match);
                Emit(CodeInstruction.StoreLocal(substituteIndex));
            }
            else if (replaceInst.opcode == OpCodes.Ldloca || replaceInst.opcode == OpCodes.Ldloca_S)
            {
                var substituteIndex = GetReplacementLocal((int)replaceInst.operand, match);
                Emit(CodeInstruction.LoadLocalAddress(substituteIndex));
            }
            else if (replaceInst.IsLdloc())
            {
                var substituteIndex = GetReplacementLocal(replaceInst.LocalIndex(), match);
                Emit(CodeInstruction.LoadLocal(substituteIndex));
            }
            else if (replaceInst.operand is Label label)
            {
                Emit(replaceInst.opcode, GetReplacementLabel(label, match));
            }
            else
                Emit(replaceInst.opcode, replaceInst.operand);

            OutInstructions[^1].labels.AddRange(replaceInst.labels
                .Select(label => GetReplacementLabel(label, match)));
        }

        private void Emit(CodeInstruction instruction)
        {
            Emit(instruction.opcode, instruction.operand, instruction.labels, instruction.blocks);
        }

        private void Emit(
            OpCode opcode,
            object? operand = null,
            List<Label>? labels = null,
            List<ExceptionBlock>? blocks = null)
        {
            CodeInstruction newInstruction = new(opcode, operand);
            if (labels != null)
                newInstruction.labels.AddRange(labels);
            if (blocks != null)
                newInstruction.blocks.AddRange(blocks);

            if (extraBlocks.Count > 0)
                newInstruction.blocks.AddRange(extraBlocks);

            OutInstructions.Add(newInstruction);
        }

        private Label GetReplacementLabel(Label label, MatchData match)
        {
            Dictionary<Label, Label> labelMap = instructionMatcher.CrossRuleLabels.Contains(label) ? labelMap_Method : match.labelMap_Match;
            if (!labelMap.TryGetValue(label, out Label replacementLabel))
            {
                replacementLabel = generator.DefineLabel();
                labelMap.Add(label, replacementLabel);
            }

            return replacementLabel;
        }

        private int GetReplacementLocal(int localIndex, MatchData match)
        {
            if (localMap_Method.TryGetValue(localIndex, out var substituteIndex))
            {
            }
            else if (match.localMap_Match.TryGetValue(localIndex, out substituteIndex))
            {
            }
            else if (localIndex < instructionMatcher.CrossRuleLocalTypes.Count)
            {
                substituteIndex = generator.DeclareLocal(instructionMatcher.CrossRuleLocalTypes[localIndex]).LocalIndex;
                localMap_Method.Add(localIndex, substituteIndex);
            }
            else if (match.rule.LocalTypes != null && localIndex < match.rule.LocalTypes.Length)
            {
                substituteIndex = generator.DeclareLocal(match.rule.LocalTypes[localIndex]).LocalIndex;
                match.localMap_Match.Add(localIndex, substituteIndex);
            }
            else
            {
                throw new InvalidOperationException($"Replacement pattern uses unknown local index #{localIndex}");
            }

            return substituteIndex;
        }
    }
}
