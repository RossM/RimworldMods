using JetBrains.Annotations;

namespace Disharmony;

public class InstructionMatcher
{
    public enum OutputMode
    {
        /// <summary>
        ///     <see cref="Rule.Pattern" /> is matched against, but no instruction changes are made.
        /// </summary>
        MatchOnly,

        /// <summary>
        ///     <see cref="Rule.Output" /> replaces the instructions that match <see cref="Rule.Pattern" />.
        /// </summary>
        Replace,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted before the first instruction of each match of the
        ///     <see cref="Rule.Pattern" />.
        /// </summary>
        InsertBefore,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted after the last instruction of each match of the <see cref="Rule.Pattern" />.
        /// </summary>
        InsertAfter,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted at the very start of the instructions. No matching is done.
        /// </summary>
        MethodPrefix,

        /// <summary>
        ///     <see cref="Rule.Output" /> is inserted at the very end of the instructions. No matching is done.
        /// </summary>
        MethodPostfix,
    }

    public class Rule
    {
        public int Min = 1, Max = 1;
        public OutputMode Mode = OutputMode.MatchOnly;
        public bool SaveLocals = false;
        public bool Chained = false;
        public CodeInstruction[]? Pattern;
        public CodeInstruction[]? Output;
        public Type[]? LocalTypes;
        public string? Name;
    }

    private class MatchData
    {
        public required Rule rule;
        public int start, end;
        public required Dictionary<int, int> localMap_Match;
        public required Dictionary<Label, Label> labelMap_Match;
        public bool emitted = false;
    }

    public static bool forceDebug = false;

    public List<Rule> Rules = [];
    public List<Type> CrossRuleLocalTypes = [];
    public List<Label> CrossRuleLabels = [];

    private readonly List<ExceptionBlock> extraBlocks = [];

    public InstructionMatcher()
    {
    }

    public InstructionMatcher(params List<Rule> rules)
    {
        Rules = rules;
    }

    public bool TryMatchAndReplace(
        MethodBase method,
        ref List<CodeInstruction> instructions,
        out string reason,
        ILGenerator generator,
        bool debug = false)
    {
        var localMap_Method = new Dictionary<int, int>();
        var labelMap_Method = new Dictionary<Label, Label>();
        var matches = new List<MatchData>();
        reason = "Success";

        debug |= forceDebug;

        if (debug)
            FileLog.Log($"## InfixPatcher {method.FullName}");

        // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
        for (var ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
        {
            Rule rule = Rules[ruleIndex];

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

            if (rule.Mode == OutputMode.MethodPrefix)
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

            if (rule.Mode == OutputMode.MethodPostfix)
            {
                matches.Add(new MatchData
                {
                    rule = rule,
                    start = instructions.Count,
                    end = instructions.Count,
                    localMap_Match = new(),
                    labelMap_Match = new(),
                });
                continue;
            }

            var matchCount = 0;

            for (int instructionIndex = rule.Chained && matches.Count > 0 ? matches[^1].end : 0;
                 instructionIndex <= instructions.Count - rule.Pattern.Length;
                 instructionIndex++)
            {
                var isMatch = true;
                var localIndex_Match = new Dictionary<int, int>();

                for (var patternIndex = 0; patternIndex < rule.Pattern.Length; patternIndex++)
                {
                    CodeInstruction inst = instructions[instructionIndex + patternIndex];
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
                if (debug)
                    FileLog.Log($"MATCH #{ruleIndex} ({matchData.start} .. {matchData.end - 1})");

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
                reason = $"Not enough matches found for substitution #{ruleIndex}";
                return false;
            }
        }

        var sortedMatches = matches.OrderBy(m => m.start).ToList();
        for (var i = 0; i < sortedMatches.Count - 1; i++)
        {
            if (sortedMatches[i].end > sortedMatches[i + 1].start)
            {
                reason = "Overlapping matches";
                return false;
            }
        }

        if (matches.Count == 0)
        {
            reason = "No matches";
            return false;
        }

        extraBlocks.Clear();

        // Make the substitutions
        var outInstructions = new List<CodeInstruction>();
        for (var instructionIndex = 0;; instructionIndex++)
        {
            int index = instructionIndex;
            var match = sortedMatches.FirstOrDefault(r => r.start == index && !r.emitted);
            match?.emitted = true;

            if (match?.rule.Output != null)
            {
                if (match.rule.Mode == OutputMode.InsertAfter)
                {
                    for (int i = match.start; i < match.end; i++)
                    {
                        Emit(outInstructions, instructions[i]);
                        if (debug)
                            FileLog.Log($"COPY MATCH {outInstructions[^1]}");
                    }
                }

                instructionIndex = match.end - 1;

                if (match.rule.Mode is OutputMode.Replace or OutputMode.InsertBefore &&
                    instructions[match.start] is { labels: { Count: > 0 } labels })
                {
                    Emit(outInstructions, new(OpCodes.Nop) { labels = labels });
                }

                Emit(outInstructions, CodeInstruction.Annotation($"Begin {match.rule.Name}"));

                extraBlocks.AddRange(instructions[match.start].blocks);

                foreach (CodeInstruction replaceInst in match.rule.Output)
                {
                    if (replaceInst.IsStloc())
                    {
                        if (!TryGetLocalIndex(ref reason, generator, localMap_Method, match, out var substituteIndex,
                                replaceInst.LocalIndex()))
                            return false;

                        Emit(outInstructions, CodeInstruction.StoreLocal(substituteIndex));
                    }
                    else if (replaceInst.opcode == OpCodes.Ldloca || replaceInst.opcode == OpCodes.Ldloca_S)
                    {
                        if (!TryGetLocalIndex(ref reason, generator, localMap_Method, match, out var substituteIndex,
                                (int)replaceInst.operand))
                            return false;

                        Emit(outInstructions, CodeInstruction.LoadLocalAddress(substituteIndex));
                    }
                    else if (replaceInst.IsLdloc())
                    {
                        if (!TryGetLocalIndex(ref reason, generator, localMap_Method, match, out var substituteIndex,
                                replaceInst.LocalIndex()))
                            return false;

                        Emit(outInstructions, CodeInstruction.LoadLocal(substituteIndex));
                    }
                    else if (replaceInst.operand is Label label)
                    {
                        Emit(outInstructions, replaceInst.opcode, GetReplacementLabel(label, generator, match, labelMap_Method));
                    }
                    else
                        Emit(outInstructions, replaceInst.opcode, replaceInst.operand);

                    outInstructions[^1].labels.AddRange(replaceInst.labels
                        .Select(label => GetReplacementLabel(label, generator, match, labelMap_Method)));

                    if (debug)
                        FileLog.Log($"EMIT {outInstructions[^1]}");
                }

                extraBlocks.Clear();

                Emit(outInstructions, CodeInstruction.Annotation($"End {match.rule.Name}"));

                if (match.rule.Mode == OutputMode.InsertBefore)
                {
                    for (int i = match.start; i < match.end; i++)
                    {
                        Emit(outInstructions, instructions[i]);

                        if (i == match.start)
                            outInstructions[^1].labels.Clear();

                        if (debug)
                            FileLog.Log($"COPY MATCH {outInstructions[^1]}");
                    }
                }
            }
            else
            {
                if (instructionIndex >= instructions.Count)
                    break;

                Emit(outInstructions, instructions[instructionIndex]);
                if (debug)
                    FileLog.Log($"COPY {outInstructions[^1]}");
            }
        }

        // Everything succeeded, now safe to change ref instructions
        instructions = outInstructions;

        if (debug)
            FileLog.Log("");

        return true;
    }

    private void Emit(List<CodeInstruction> outInstructions, CodeInstruction instruction)
    {
        Emit(outInstructions, instruction.opcode, instruction.operand, instruction.labels, instruction.blocks);
    }

    private void Emit(
        List<CodeInstruction> outInstructions,
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

        outInstructions.Add(newInstruction);
    }

    private Label GetReplacementLabel(Label label, ILGenerator generator, MatchData match, Dictionary<Label, Label> labelMap_Method)
    {
        Dictionary<Label, Label> labelMap = CrossRuleLabels.Contains(label) ? labelMap_Method : match.labelMap_Match;
        if (!labelMap.TryGetValue(label, out Label replacementLabel))
        {
            replacementLabel = generator.DefineLabel();
            labelMap.Add(label, replacementLabel);
        }

        return replacementLabel;
    }

    private bool TryGetLocalIndex(
        ref string reason,
        ILGenerator generator,
        Dictionary<int, int> localIndex_Method,
        MatchData match,
        out int substituteIndex,
        int localIndex)
    {
        bool valid = true;
        if (localIndex_Method.TryGetValue(localIndex, out substituteIndex))
        {
        }
        else if (match.localMap_Match.TryGetValue(localIndex, out substituteIndex))
        {
        }
        else if (localIndex < CrossRuleLocalTypes.Count)
        {
            substituteIndex = generator.DeclareLocal(CrossRuleLocalTypes[localIndex]).LocalIndex;
            localIndex_Method.Add(localIndex, substituteIndex);
        }
        else if (match.rule.LocalTypes != null && localIndex < match.rule.LocalTypes.Length)
        {
            substituteIndex = generator.DeclareLocal(match.rule.LocalTypes[localIndex]).LocalIndex;
            match.localMap_Match.Add(localIndex, substituteIndex);
        }
        else
        {
            reason = $"Replacement pattern uses unknown local index #{localIndex}";
            valid = false;
        }

        return valid;
    }

    public void MatchAndReplace(
        MethodBase method,
        ref List<CodeInstruction> instructionsList,
        ILGenerator generator,
        [CallerMemberName] string? methodName = null,
        bool debug = false)
    {
        if (!TryMatchAndReplace(method, ref instructionsList, out string reason, generator, debug))
            throw new InvalidOperationException(reason);
    }

    public static List<CodeInstruction> MatchAndReplace(
        List<Rule> rules,
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        bool debug = false)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        new InstructionMatcher(rules).MatchAndReplace(method, ref instructionsList, generator, debug: debug);
        return instructionsList;
    }

    [UsedImplicitly]
    public static List<CodeInstruction> RunMatchers(
        InstructionMatcher[] matchers,
        MethodBase target,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        var instructionsList = instructions.ToList();
        foreach (var matcher in matchers)
        {
            matcher.MatchAndReplace(target, ref instructionsList, generator);
        }

        return instructionsList;
    }

    public static Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember)
    {
        return new Rule
        {
            Min = 1,
            Max = 0,
            Mode = OutputMode.Replace,
            Pattern = [new(OpCodes.Call, oldMember)],
            Output = [new(OpCodes.Call, newMember)],
            LocalTypes = [],
            Name = oldMember.FullName,
        };
    }
}
