using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace TranspilerUtil;

public class InstructionMatcher
{
    public enum OutputMode
    {
        MatchOnly,
        Replace,
        InsertBefore,
        InsertAfter,
    }

    public class Rule
    {
        public int Min = 1, Max = 1;
        public OutputMode Mode = OutputMode.MatchOnly;
        public bool SaveLocals = false;
        public bool Chained = false;
        public CodeInstruction[] Pattern;
        public CodeInstruction[] Output;
        public Func<MethodBase, List<CodeInstruction>, ILGenerator, Rule> LateGenerator;
        public Type[] LocalTypes;
    }

    private class MatchData
    {
        public Rule rule;
        public int start, end;
        public Dictionary<int, int> privateMap;
        public Dictionary<Label, Label> labelMap;
    }

    public static bool forceDebug = false;

    public List<Rule> Rules = [];
    public List<Type> LocalTypes = [];

    readonly List<Label> extraLabels = [];
    private readonly List<ExceptionBlock> extraBlocks = [];

    public bool TryMatchAndReplace(
        MethodBase method,
        ref List<CodeInstruction> instructions,
        out string reason,
        ILGenerator generator = null,
        bool debug = false)
    {
        var localIndexMap = new Dictionary<int, int>();
        var matches = new List<MatchData>();
        reason = "Success";

        debug |= forceDebug;

        foreach (var rule in Rules)
        {
            if (rule.Mode == OutputMode.MatchOnly && rule.Output != null)
                throw new InvalidOperationException($"{rule.Mode} rule cannot have Output = null");
            if (rule.Mode != OutputMode.MatchOnly && rule.Output == null)
                throw new InvalidOperationException($"{rule.Mode} rule must have Output = null");
        }

        // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
        for (var ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
        {
            Rule rule = Rules[ruleIndex];
            if (rule.LateGenerator != null)
                rule = rule.LateGenerator(method, instructions, generator);
            var matchCount = 0;

            for (int instructionIndex = rule.Chained && matches.Count > 0 ? matches[^1].end + 1 : 0;
                 instructionIndex <= instructions.Count - rule.Pattern.Length;
                 instructionIndex++)
            {
                var isMatch = true;
                var tempLocalIndexMap = new Dictionary<int, int>();

                for (var patternIndex = 0; patternIndex < rule.Pattern.Length; patternIndex++)
                {
                    var inst = instructions[instructionIndex + patternIndex];
                    var patternInst = rule.Pattern[patternIndex];

                    //if (debug)
                    //    Debug.Log($"COMPARE {patternInst} : {inst}");

                    // For a load or store, map the local indexes in the pattern to the actual local indexes used
                    // in the function
                    if (patternInst.IsStloc())
                    {
                        isMatch = inst.IsStloc();
                        if (!isMatch)
                            break;

                        int localIndex = patternInst.LocalIndex();
                        int targetIndex = inst.LocalIndex();

                        if (localIndexMap.TryGetValue(localIndex, out int substituteIndex))
                            isMatch = targetIndex == substituteIndex;
                        else if (tempLocalIndexMap.TryGetValue(localIndex, out substituteIndex))
                            isMatch = targetIndex == substituteIndex;
                        else
                            tempLocalIndexMap.Add(localIndex, targetIndex);
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

                        if (localIndexMap.TryGetValue(localIndex, out int substituteIndex))
                            isMatch = targetIndex == substituteIndex;
                        else if (tempLocalIndexMap.TryGetValue(localIndex, out substituteIndex))
                            isMatch = targetIndex == substituteIndex;
                        else
                            tempLocalIndexMap.Add(localIndex, targetIndex);
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

                var matchData = new MatchData()
                {
                    rule = rule,
                    start = instructionIndex,
                    end = instructionIndex + rule.Pattern.Length - 1,
                    privateMap = tempLocalIndexMap,
                    labelMap = new(),
                };
                if (debug)
                    Debug.Log($"MATCH #{ruleIndex} {matchData.start}-{matchData.end}");

                matches.Add(matchData);
                if (rule.SaveLocals)
                    localIndexMap.AddRange(tempLocalIndexMap);
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
            if (sortedMatches[i].end >= sortedMatches[i + 1].start)
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

        extraLabels.Clear();
        extraBlocks.Clear();

        // Make the substitutions
        var outInstructions = new List<CodeInstruction>();
        for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
        {
            int index = instructionIndex;
            var match = sortedMatches.FirstOrDefault(r => r.start == index);

            if (match?.rule.Output != null)
            {
                if (match.rule.Mode == OutputMode.InsertAfter)
                {
                    for (int i = match.start; i <= match.end; i++)
                    {
                        Emit(outInstructions, instructions[i]);
                        if (debug)
                            Debug.Log($"COPY MATCH {outInstructions[^1]}");
                    }
                }

                instructionIndex = match.end;

                if (match.rule.Mode is OutputMode.Replace or OutputMode.InsertBefore)
                {
                    extraLabels.AddRange(instructions[match.start].labels);
                }

                extraBlocks.AddRange(instructions[match.start].blocks);

                foreach (CodeInstruction replaceInst in match.rule.Output)
                {
                    if (replaceInst.IsStloc())
                    {
                        if (!TryGetLocalIndex(ref reason, generator, localIndexMap, match, out var substituteIndex,
                                replaceInst.LocalIndex()))
                            return false;

                        Emit(outInstructions, CodeInstruction.StoreLocal(substituteIndex));
                    }
                    else if (replaceInst.opcode == OpCodes.Ldloca || replaceInst.opcode == OpCodes.Ldloca_S)
                    {
                        if (!TryGetLocalIndex(ref reason, generator, localIndexMap, match, out var substituteIndex,
                                (int)replaceInst.operand))
                            return false;

                        Emit(outInstructions, CodeInstructionUtil.LoadLocalAddress(substituteIndex));
                    }
                    else if (replaceInst.IsLdloc())
                    {
                        if (!TryGetLocalIndex(ref reason, generator, localIndexMap, match, out var substituteIndex,
                                replaceInst.LocalIndex()))
                            return false;

                        Emit(outInstructions, CodeInstruction.LoadLocal(substituteIndex));
                    }
                    else if (replaceInst.opcode == OpCodes.Nop)
                    {
                        extraLabels.AddRange(replaceInst.labels.Select(label => GetReplacementLabel(generator, match, label)));

                        if (debug)
                            Debug.Log($"SKIP {replaceInst}");
                        continue;
                    }
                    else if (replaceInst.operand is Label label)
                    {
                        Emit(outInstructions, replaceInst.opcode, GetReplacementLabel(generator, match, label));
                    }
                    else
                        Emit(outInstructions, replaceInst.opcode, replaceInst.operand);

                    outInstructions[^1].labels.AddRange(replaceInst.labels
                        .Select(label => GetReplacementLabel(generator, match, label)));

                    if (debug)
                        Debug.Log($"EMIT {outInstructions[^1]}");
                }

                extraBlocks.Clear();

                if (match.rule.Mode == OutputMode.InsertBefore)
                {
                    for (int i = match.start; i <= match.end; i++)
                    {
                        Emit(outInstructions, instructions[i]);

                        if (i == match.start)
                            outInstructions[^1].labels.Clear();

                        if (debug)
                            Debug.Log($"COPY MATCH {outInstructions[^1]}");
                    }
                }
            }
            else
            {
                Emit(outInstructions, instructions[instructionIndex]);
                if (debug)
                    Debug.Log($"COPY {outInstructions[^1]}");
            }
        }

        // Everything succeeded, now safe to change ref instructions
        instructions = outInstructions;
        return true;
    }

    private void Emit(List<CodeInstruction> outInstructions, CodeInstruction instruction)
    {
        Emit(outInstructions, instruction.opcode, instruction.operand, instruction.labels, instruction.blocks);
    }

    private void Emit(
        List<CodeInstruction> outInstructions,
        OpCode opcode,
        object operand = null,
        List<Label> labels = null,
        List<ExceptionBlock> blocks = null)
    {
        CodeInstruction newInstruction = new(opcode, operand);
        if (labels != null)
            newInstruction.labels.AddRange(labels);
        if (blocks != null)
            newInstruction.blocks.AddRange(blocks);
        if (extraLabels.Count > 0)
        {
            newInstruction.labels.AddRange(extraLabels);
            extraLabels.Clear();
        }
        if (extraBlocks.Count > 0)
            newInstruction.blocks.AddRange(extraBlocks);

        outInstructions.Add(newInstruction);
    }

    private static Label GetReplacementLabel(ILGenerator generator, MatchData match, Label label)
    {
        if (!match.labelMap.TryGetValue(label, out Label replacementLabel))
        {
            replacementLabel = generator.DefineLabel();
            //Debug.Log($"Label{label.GetHashCode()} -> Label{replacementLabel.GetHashCode()}");
            match.labelMap.Add(label, replacementLabel);
        }

        return replacementLabel;
    }

    private bool TryGetLocalIndex(
        ref string reason,
        ILGenerator generator,
        Dictionary<int, int> localIndexMap,
        MatchData match,
        out int substituteIndex,
        int localIndex)
    {
        bool valid = true;
        if (localIndexMap.TryGetValue(localIndex, out substituteIndex))
        {
        }
        else if (match.privateMap.TryGetValue(localIndex, out substituteIndex))
        {
        }
        else if (match.rule.LocalTypes != null && localIndex < match.rule.LocalTypes.Length && generator != null)
        {
            substituteIndex = generator.DeclareLocal(match.rule.LocalTypes[localIndex]).LocalIndex;
            match.privateMap.Add(localIndex, substituteIndex);
        }
        else if (LocalTypes != null && localIndex < LocalTypes.Count && generator != null)
        {
            substituteIndex = generator.DeclareLocal(LocalTypes[localIndex]).LocalIndex;
            localIndexMap.Add(localIndex, substituteIndex);
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
        ILGenerator generator = null,
        [CallerMemberName] string methodName = null,
        bool debug = false)
    {
        if (!TryMatchAndReplace(method, ref instructionsList, out string reason, generator, debug))
            Log.Error($"{methodName ?? "<Unknown>"}: {reason}");
    }

    [UsedImplicitly]
    public static List<CodeInstruction> MatchAndReplace(
        List<Rule> rules,
        MethodBase method,
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        bool debug)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        new InstructionMatcher() { Rules = rules }.MatchAndReplace(method, ref instructionsList, generator, debug: debug);
        return instructionsList;
    }
}
