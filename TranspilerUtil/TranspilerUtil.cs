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
using OpCodes = System.Reflection.Emit.OpCodes;

namespace TranspilerUtil
{
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

                for (int instructionIndex = rule.Chained && matches.Count > 0 ? matches[matches.Count - 1].end + 1 : 0;
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
                            outInstructions.Add(instructions[i]);
                            if (debug)
                                Debug.Log($"COPYMATCH {outInstructions[outInstructions.Count - 1]}");
                        }
                    }

                    instructionIndex = match.end;

                    for (var i = 0; i < match.rule.Output.Length; i++)
                    {
                        CodeInstruction replaceInst = match.rule.Output[i];
                        if (replaceInst.IsStloc())
                        {
                            if (!TryGetLocalIndex(ref reason, generator, localIndexMap, match, out var substituteIndex,
                                    replaceInst.LocalIndex()))
                                return false;

                            outInstructions.Add(CodeInstruction.StoreLocal(substituteIndex));
                        }
                        else if (replaceInst.opcode == OpCodes.Ldloca || replaceInst.opcode == OpCodes.Ldloca_S)
                        {
                            if (!TryGetLocalIndex(ref reason, generator, localIndexMap, match, out var substituteIndex,
                                    (int)replaceInst.operand))
                                return false;

                            outInstructions.Add(new(OpCodes.Ldloca, substituteIndex));
                        }
                        else if (replaceInst.IsLdloc())
                        {
                            if (!TryGetLocalIndex(ref reason, generator, localIndexMap, match, out var substituteIndex,
                                    replaceInst.LocalIndex()))
                                return false;

                            outInstructions.Add(CodeInstruction.LoadLocal(substituteIndex));
                        }
                        else if (replaceInst.operand is Label label)
                        {
                            outInstructions.Add(new(replaceInst.opcode, GetReplacementLabel(generator, match, label)));
                        }
                        else
                            outInstructions.Add(new(replaceInst.opcode, replaceInst.operand));

                        outInstructions[outInstructions.Count - 1].labels = replaceInst.labels
                            .Select(label => GetReplacementLabel(generator, match, label)).ToList();

                        if (i == 0 && match.rule.Mode == OutputMode.Replace)
                        {
                            outInstructions[outInstructions.Count - 1].labels.AddRange(instructions[match.start].labels);
                        }

                        if (debug)
                            Debug.Log($"EMIT {outInstructions[outInstructions.Count - 1]}");
                    }

                    if (match.rule.Mode == OutputMode.InsertBefore)
                    {
                        for (int i = match.start; i <= match.end; i++)
                        {
                            outInstructions.Add(instructions[i]);
                            if (debug)
                                Debug.Log($"COPYMATCH {outInstructions[outInstructions.Count - 1]}");
                        }
                    }
                }
                else
                {
                    outInstructions.Add(instructions[instructionIndex]);
                    if (debug)
                        Debug.Log($"COPY {outInstructions[outInstructions.Count - 1]}");
                }
            }

            // Everything succeeded, now safe to change ref instructions
            instructions = outInstructions;
            return true;
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
            ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher() { Rules = rules }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }
    }
}
