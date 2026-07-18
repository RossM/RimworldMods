namespace Disharmony;

public partial class InstructionMatcher
{
    private class Worker(
        InstructionMatcher instructionMatcher,
        MethodBase method,
        List<CodeInstruction> inInstructions,
        ILGenerator generator,
        bool debug)
    {
        private readonly Dictionary<int, int> localMap_Method = new();
        private readonly Dictionary<Label, Label> labelMap_Method = new();
        private readonly List<MatchData> matches = [];
        private readonly List<ExceptionBlock> extraBlocks = [];
        private readonly List<Label> extraLabels = [];

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
                        throw new InvalidOperationException($"{rule.Mode} rule must have Output = null");
                    case { Mode: not OutputMode.MatchOnly, Output: null }:
                        throw new InvalidOperationException($"{rule.Mode} rule cannot have Output = null");
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

                // rule.Pattern was checked to be non-null during the error checking above
                if (rule.Pattern is null)
                    throw new InvalidOperationException();

                for (int instructionIndex = rule.Chained && matches.Count > 0 ? matches[^1].end : 0;
                     instructionIndex <= inInstructions.Count - rule.Pattern.Length;
                     instructionIndex++)
                {
                    if (!MatchPattern(rule, instructionIndex, out Dictionary<int, int> localIndex_Match))
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

                    continue;
                }

                match.emitted = true;
                instructionIndex = match.end - 1;

                if (match.start == match.end)
                {
                    EmitReplacement(match);
                    continue;
                }

                static bool IsBlockStart(ExceptionBlock b) => b.blockType != ExceptionBlockType.EndExceptionBlock;
                static bool IsBlockEnd(ExceptionBlock b) => b.blockType == ExceptionBlockType.EndExceptionBlock;

                switch (match.rule.Mode)
                {
                    case OutputMode.Replace:
                    {
                        if (inInstructions[match.start].labels.Count > 0 || inInstructions[match.start].blocks.Any(IsBlockStart))
                        {
                            Emit(OpCodes.Nop,
                                labels: inInstructions[match.start].labels,
                                blocks: inInstructions[match.start].blocks.Where(IsBlockStart).ToList());
                        }

                        EmitReplacement(match);

                        if (inInstructions[match.end - 1].blocks.Any(IsBlockEnd))
                        {
                            Emit(OpCodes.Nop,
                                blocks: inInstructions[match.end - 1].blocks.Where(IsBlockEnd).ToList());
                        }

                        break;
                    }
                    case OutputMode.InsertBefore:
                    {
                        if (inInstructions[match.start].labels.Count > 0 || inInstructions[match.start].blocks.Any(IsBlockStart))
                        {
                            Emit(OpCodes.Nop,
                                labels: inInstructions[match.start].labels,
                                blocks: inInstructions[match.start].blocks.Where(IsBlockStart).ToList());
                        }

                        EmitReplacement(match);

                        for (int i = match.start; i < match.end; i++)
                        {
                            Emit(inInstructions[i]);

                            if (i == match.start)
                            {
                                OutInstructions[^1].labels.Clear();
                                OutInstructions[^1].blocks.RemoveAll(IsBlockStart);
                            }
                        }

                        break;
                    }
                    case OutputMode.InsertAfter:
                    {
                        for (int i = match.start; i < match.end; i++)
                        {
                            Emit(inInstructions[i]);

                            if (i == match.end - 1)
                            {
                                OutInstructions[^1].blocks.RemoveAll(IsBlockEnd);
                            }
                        }

                        EmitReplacement(match);

                        if (inInstructions[match.end - 1].blocks.Any(IsBlockEnd))
                        {
                            Emit(OpCodes.Nop,
                                blocks: inInstructions[match.end - 1].blocks.Where(IsBlockEnd).ToList());
                        }

                        break;
                    }
                    default: throw new InvalidOperationException();
                }
            }

            if (debug || forceDebug)
            {
                LogInstructions();
                FileLog.Log("");
            }
        }

        private void LogInstructions()
        {
            int codePos = 0;

            foreach (var codeInstruction in OutInstructions)
            {
                codeInstruction.labels.Do(label => FileLog.LogIL(codePos, label));
                codeInstruction.blocks.Do(block => FileLog.LogILBlockBegin(codePos, block));

                var code = codeInstruction.opcode;
                var operand = codeInstruction.operand;

                var realCode = true;
                switch (code.OperandType)
                {
                    case OperandType.InlineNone:
                        if (code == OpCodes.Nop && operand is string s)
                        {
                            FileLog.LogILComment(codePos, s);
                            realCode = false;
                        }
                        else
                            FileLog.LogIL(codePos, code);

                        break;

                    //case OperandType.InlineSig:
                    //    FileLog.LogIL(codePos, code, (ICallSiteGenerator)operand);
                    //    break;

                    default: FileLog.LogIL(codePos, code, operand); break;
                }

                codeInstruction.blocks.Do(block => FileLog.LogILBlockEnd(codePos, block));
                if (realCode)
                    codePos += ILSize(codeInstruction.opcode);
            }

            FileLog.FlushBuffer();
        }

        private static int ILSize(OpCode opCode)
        {
            int size = opCode.Size;
            size += opCode.OperandType switch
            {
                OperandType.InlineNone => 0,
                OperandType.ShortInlineBrTarget => 1,
                OperandType.ShortInlineI => 1,
                OperandType.ShortInlineVar => 1,
                OperandType.InlineVar => 2,
                OperandType.InlineI8 => 8,
                OperandType.InlineR => 8,
                _ => 4,
            };
            return size;
        }

        private void EmitReplacement(MatchData match)
        {
            Emit(CodeInstruction.Annotation($"Begin {match.rule.Name}"));

            // Rules with null Output aren't added to matches so should never get here
            if (match.rule.Output is null)
                throw new InvalidOperationException();

            foreach (CodeInstruction replaceInst in match.rule.Output)
                EmitReplacement(replaceInst, match);

            Emit(CodeInstruction.Annotation($"End {match.rule.Name}"));
        }

        private bool MatchPattern(Rule rule, int instructionIndex, out Dictionary<int, int> localIndex_Match)
        {
            localIndex_Match = new Dictionary<int, int>();

            bool noOutput = rule.Output is not { Length: > 0 };

            // rule.Pattern is checked to be non-null before this is called
            if (rule.Pattern is null)
                throw new InvalidOperationException();

            for (var patternIndex = 0; patternIndex < rule.Pattern.Length; patternIndex++)
            {
                if (!MatchInstruction(inInstructions[instructionIndex + patternIndex], rule.Pattern[patternIndex], localIndex_Match))
                    return false;

                if (rule.Mode == OutputMode.Replace)
                {
                    // Check for exception blocks or labels not at the start (or end, for EndExceptionBlock) of the match
                    CodeInstruction inst = inInstructions[instructionIndex + patternIndex];
                    if ((patternIndex > 0 || noOutput) && inst.blocks.Any(b => b.blockType != ExceptionBlockType.EndExceptionBlock))
                        return false;
                    if ((patternIndex > 0 || noOutput) && inst.labels.Count > 0)
                        return false;
                    if ((patternIndex < rule.Pattern.Length - 1 || noOutput) &&
                        inst.blocks.Any(b => b.blockType == ExceptionBlockType.EndExceptionBlock))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool MatchInstruction(CodeInstruction inst, CodeInstruction patternInst, Dictionary<int, int> localIndex_Match)
        {
            // For a load or store, map the local indexes in the pattern to the actual local indexes used
            // in the function
            if (patternInst.IsStloc())
            {
                if (!inst.IsStloc())
                    return false;

                int localIndex = patternInst.LocalIndex();
                int targetIndex = inst.LocalIndex();

                if (localMap_Method.TryGetValue(localIndex, out int substituteIndex) ||
                    localIndex_Match.TryGetValue(localIndex, out substituteIndex))
                {
                    if (targetIndex != substituteIndex)
                        return false;
                }
                else
                {
                    localIndex_Match.Add(localIndex, targetIndex);
                }
            }
            else if (patternInst.opcode.Value == OpCodes.Ldloca.Value ||
                     patternInst.opcode.Value == OpCodes.Ldloca_S.Value)
            {
                if (inst.opcode != patternInst.opcode)
                    return false;

                throw new NotSupportedException();
            }
            else if (patternInst.IsLdloc())
            {
                if (!inst.IsLdloc() ||
                    inst.opcode.Value == OpCodes.Ldloca.Value ||
                    inst.opcode.Value == OpCodes.Ldloca_S.Value)
                    return false;

                int localIndex = patternInst.LocalIndex();

                // There is something very weird going on here. This may be a Harmony bug.
                int targetIndex = inst.operand is LocalBuilder lb ? lb.LocalIndex : inst.LocalIndex();

                if (localMap_Method.TryGetValue(localIndex, out int substituteIndex) ||
                    localIndex_Match.TryGetValue(localIndex, out substituteIndex))
                {
                    if (targetIndex != substituteIndex)
                        return false;
                }
                else
                {
                    localIndex_Match.Add(localIndex, targetIndex);
                }
            }
            // For convenience, let call also match callvirt. Nobody wants to worry about
            // the difference when writing patterns.
            else if (patternInst.opcode.Value == OpCodes.Call.Value)
            {
                if (inst.opcode.Value != OpCodes.Call.Value &&
                    inst.opcode.Value != OpCodes.Callvirt.Value ||
                    !inst.operand.Equals(patternInst.operand))
                    return false;
            }
            else if (patternInst.operand == null)
            {
                if (inst.opcode.Value != patternInst.opcode.Value || inst.operand != null)
                    return false;
            }
            else
            {
                if (!inst.Is(patternInst.opcode, patternInst.operand))
                    return false;
            }

            return true;
        }

        private void EmitReplacement(CodeInstruction replaceInst, MatchData match)
        {
            if (replaceInst.labels.Count > 0)
                extraLabels.AddRange(replaceInst.labels.Select(label => GetReplacementLabel(label, match)));

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
            {
                newInstruction.blocks.AddRange(extraBlocks);
                extraBlocks.Clear();
            }

            if (extraLabels.Count > 0)
            {
                newInstruction.labels.AddRange(extraLabels);
                extraLabels.Clear();
            }

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
            if (localMap_Method.TryGetValue(localIndex, out var substituteIndex)) { }
            else if (match.localMap_Match.TryGetValue(localIndex, out substituteIndex)) { }
            else if (localIndex < instructionMatcher.CrossRuleLocalTypes.Count)
            {
                substituteIndex = generator.DeclareLocal(instructionMatcher.CrossRuleLocalTypes[localIndex]).LocalIndex;
                localMap_Method.Add(localIndex, substituteIndex);
            }
            else
            {
                throw new InvalidOperationException($"Replacement pattern uses unknown local index #{localIndex}");
            }

            return substituteIndex;
        }
    }
}
