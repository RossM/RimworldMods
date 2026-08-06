namespace Disharmony.RuleEngine;

internal class Processor(
    Ruleset ruleset,
    MethodBase method,
    List<CodeInstruction> inInstructions,
    ILGenerator generator,
    bool debug)
{
    internal class MatchData
    {
        public required Rule rule;
        public int start, end;
        public required Dictionary<int, int> localMap_Match;
        public required Dictionary<Label, Label> labelMap_Match;
        public bool emitted = false;
    }

    private readonly Dictionary<int, LocalBuilder> localMap_Method = [];
    private readonly Dictionary<Label, Label> labelMap_Method = [];
    private readonly List<MatchData> matches = [];
    private readonly List<ExceptionBlock> extraBlocks = [];
    private readonly List<Label> extraLabels = [];

    public List<CodeInstruction> outInstructions = [];
    private List<CodeInstruction> instructions = inInstructions;

    public void MatchAndReplace()
    {
        if (debug)
            FileLog.Log($"## InfixPatcher {method.FullName}");

        foreach (var phase in ruleset.rules.GroupBy(r => r.phase).OrderBy(p => p.Key))
        {
            matches.Clear();
            extraBlocks.Clear();
            extraLabels.Clear();
            outInstructions = [];

            // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
            foreach (Rule rule in phase)
            {
                switch (rule)
                {
                    case { mode: OutputMode.MethodPrefix or OutputMode.MethodPostfix, pattern.Length: > 0 }:
                        throw new InvalidOperationException($"{rule.mode} cannot have a Pattern");
                    case { mode: not (OutputMode.MethodPrefix or OutputMode.MethodPostfix), pattern: not { Length: > 0 } }:
                        throw new InvalidOperationException($"{rule.mode} rule must have a Pattern");
                }

                switch (rule.mode)
                {
                    case OutputMode.MethodPrefix:
                    {
                        matches.Add(new MatchData
                        {
                            rule = rule,
                            start = 0,
                            end = 0,
                            localMap_Match = [],
                            labelMap_Match = [],
                        });
                        continue;
                    }
                    case OutputMode.MethodPostfix:
                    {
                        matches.Add(new MatchData
                        {
                            rule = rule,
                            start = instructions.Count,
                            end = instructions.Count,
                            localMap_Match = [],
                            labelMap_Match = [],
                        });
                        continue;
                    }
                }

                var matchCount = 0;

                // rule.Pattern was checked to be non-null during the error checking above
                if (rule.pattern is null)
                    throw new InvalidOperationException();

                for (int instructionIndex = 0;
                     instructionIndex <= instructions.Count - rule.pattern.Length;
                     instructionIndex++)
                {
                    if (!MatchPattern(rule, instructionIndex, out Dictionary<int, int> localIndex_Match))
                        continue;

                    var matchData = new MatchData
                    {
                        rule = rule,
                        start = instructionIndex,
                        end = instructionIndex + rule.pattern.Length,
                        localMap_Match = localIndex_Match,
                        labelMap_Match = [],
                    };
                    if (debug)
                        FileLog.Log($"MATCH {rule.name} ({matchData.start} .. {matchData.end - 1})");

                    if (rule.output != null)
                        matches.Add(matchData);

                    matchCount++;
                    if (rule.max > 0 && matchCount >= rule.max)
                        break;
                }

                if (matchCount < rule.min)
                {
                    throw new InvalidOperationException($"Not enough matches found for substitution {rule.name}");
                }
            }

            var sortedMatches = matches.OrderBy(m => m.start).ThenByDescending(m => m.rule.priority).ToList();
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
                    if (instructionIndex >= instructions.Count)
                        break;

                    Emit(instructions[instructionIndex]);

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

                switch (match.rule.mode)
                {
                    case OutputMode.Replace:
                    {
                        if (instructions[match.start].labels.Count > 0 || instructions[match.start].blocks.Any(IsBlockStart))
                        {
                            Emit(OpCodes.Nop,
                                labels: instructions[match.start].labels,
                                blocks: [.. instructions[match.start].blocks.Where(IsBlockStart)]);
                        }

                        EmitReplacement(match);

                        if (instructions[match.end - 1].blocks.Any(IsBlockEnd))
                        {
                            Emit(OpCodes.Nop,
                                blocks: [.. instructions[match.end - 1].blocks.Where(IsBlockEnd)]);
                        }

                        break;
                    }
                    case OutputMode.InsertBefore:
                    {
                        if (instructions[match.start].labels.Count > 0 || instructions[match.start].blocks.Any(IsBlockStart))
                        {
                            Emit(OpCodes.Nop,
                                labels: instructions[match.start].labels,
                                blocks: [.. instructions[match.start].blocks.Where(IsBlockStart)]);
                        }

                        EmitReplacement(match);

                        for (int i = match.start; i < match.end; i++)
                        {
                            Emit(instructions[i]);

                            if (i == match.start)
                            {
                                outInstructions[^1].labels.Clear();
                                outInstructions[^1].blocks.RemoveAll(IsBlockStart);
                            }
                        }

                        break;
                    }
                    case OutputMode.InsertAfter:
                    {
                        for (int i = match.start; i < match.end; i++)
                        {
                            Emit(instructions[i]);

                            if (i == match.end - 1)
                            {
                                outInstructions[^1].blocks.RemoveAll(IsBlockEnd);
                            }
                        }

                        EmitReplacement(match);

                        if (instructions[match.end - 1].blocks.Any(IsBlockEnd))
                        {
                            Emit(OpCodes.Nop,
                                blocks: [.. instructions[match.end - 1].blocks.Where(IsBlockEnd)]);
                        }

                        break;
                    }
                    default: throw new InvalidOperationException();
                }
            }

            instructions = outInstructions;
        }

        if (debug)
        {
            LogInstructions();
            FileLog.Log("");
        }
    }

    private void LogInstructions()
    {
        int codePos = 0;

        foreach (var codeInstruction in outInstructions)
        {
            foreach (var label in codeInstruction.labels)
                FileLog.LogIL(codePos, label);
            foreach (var block in codeInstruction.blocks)
                FileLog.LogILBlockBegin(codePos, block);

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

            foreach (var block in codeInstruction.blocks)
                FileLog.LogILBlockEnd(codePos, block);
            if (realCode)
                codePos += ReflectionTools.ILSize(codeInstruction.opcode);
        }

        FileLog.FlushBuffer();
    }

    private void EmitReplacement(MatchData match)
    {
        Emit(CodeInstruction.Annotation($"Begin {match.rule.name}"));

        // Rules with null Output aren't added to matches so should never get here
        if (match.rule.output is null)
            throw new InvalidOperationException();

        foreach (CodeInstruction replaceInst in match.rule.output)
            EmitReplacement(replaceInst, match);

        Emit(CodeInstruction.Annotation($"End {match.rule.name}"));
    }

    private bool MatchPattern(Rule rule, int instructionIndex, out Dictionary<int, int> localIndex_Match)
    {
        localIndex_Match = [];

        bool noOutput = rule.output is not { Length: > 0 };

        // rule.Pattern is checked to be non-null before this is called
        if (rule.pattern is null)
            throw new InvalidOperationException();

        for (var patternIndex = 0; patternIndex < rule.pattern.Length; patternIndex++)
        {
            if (!MatchInstruction(instructions[instructionIndex + patternIndex], rule.pattern[patternIndex], localIndex_Match))
                return false;

            if (rule.mode == OutputMode.Replace)
            {
                // Check for exception blocks or labels not at the start (or end, for EndExceptionBlock) of the match
                CodeInstruction inst = instructions[instructionIndex + patternIndex];
                if ((patternIndex > 0 || noOutput) && inst.blocks.Any(b => b.blockType != ExceptionBlockType.EndExceptionBlock))
                    return false;
                if ((patternIndex > 0 || noOutput) && inst.labels.Count > 0)
                    return false;
                if ((patternIndex < rule.pattern.Length - 1 || noOutput) &&
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

            if (localIndex_Match.TryGetValue(localIndex, out int substituteIndex))
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

            if (localIndex_Match.TryGetValue(localIndex, out int substituteIndex))
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
        if (replaceInst.blocks.Count > 0)
            extraBlocks.AddRange(replaceInst.blocks);

        if (replaceInst.labels.Count > 0)
            extraLabels.AddRange(replaceInst.labels.Select(label => GetReplacementLabel(label, match)));

        if (replaceInst.IsStloc())
        {
            var substituteLocal = GetReplacementLocal(replaceInst.LocalIndex(), match);
            Emit(StoreLocal(substituteLocal));
        }
        else if (replaceInst.opcode == OpCodes.Ldloca || replaceInst.opcode == OpCodes.Ldloca_S)
        {
            var substituteLocal = GetReplacementLocal(replaceInst.LocalIndex(), match);
            Emit(LoadLocal(substituteLocal, true));
        }
        else if (replaceInst.IsLdloc())
        {
            var substituteLocal = GetReplacementLocal(replaceInst.LocalIndex(), match);
            Emit(LoadLocal(substituteLocal));
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

        outInstructions.Add(newInstruction);
    }

    private Label GetReplacementLabel(Label label, MatchData match)
    {
        Dictionary<Label, Label> labelMap = ruleset.crossRuleLabels.Contains(label) ? labelMap_Method : match.labelMap_Match;
        if (!labelMap.TryGetValue(label, out Label replacementLabel))
        {
            replacementLabel = generator.DefineLabel();
            labelMap.Add(label, replacementLabel);
        }

        return replacementLabel;
    }

    private object GetReplacementLocal(int localIndex, MatchData match)
    {
        if (localMap_Method.TryGetValue(localIndex, out var substituteBuilder))
            return substituteBuilder;
        if (match.localMap_Match.TryGetValue(localIndex, out int substituteIndex))
            return substituteIndex;
        if (localIndex < ruleset.crossRuleLocalTypes.Count)
        {
            substituteBuilder = generator.DeclareLocal(ruleset.crossRuleLocalTypes[localIndex]);
            localMap_Method.Add(localIndex, substituteBuilder);
            return substituteBuilder;
        }

        throw new InvalidOperationException($"Replacement pattern uses unknown local index #{localIndex}");
    }

    private static CodeInstruction StoreLocal(object local) => local switch
    {
        LocalBuilder builder => new(
            builder.LocalIndex <= byte.MaxValue ? OpCodes.Stloc_S : OpCodes.Stloc,
            builder),
        int index => CodeInstruction.StoreLocal(index),
        _ => throw new ArgumentOutOfRangeException(nameof(local)),
    };

    private static CodeInstruction LoadLocal(object local, bool useAddress = false) => local switch
    {
        LocalBuilder builder when useAddress => new(
            builder.LocalIndex <= byte.MaxValue ? OpCodes.Ldloca_S : OpCodes.Ldloca,
            builder),
        LocalBuilder builder => new(
            builder.LocalIndex <= byte.MaxValue ? OpCodes.Ldloc_S : OpCodes.Ldloc,
            builder),
        int index => CodeInstruction.LoadLocal(index, useAddress),
        _ => throw new ArgumentOutOfRangeException(nameof(local)),
    };
}
