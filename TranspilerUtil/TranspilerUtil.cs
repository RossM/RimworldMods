using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Verse;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace TranspilerUtil
{
    public class InstructionMatcher
    {
        public enum OutputMode
        {
            Replace,
            InsertBefore,
            InsertAfter,
        }

        public class Rule
        {
            public int Min = 1, Max = 1;
            public OutputMode Mode = OutputMode.InsertAfter;
            public bool SaveLocals = false;
            public bool Chained = false;
            public CodeInstruction[] Pattern;
            public CodeInstruction[] Output;
            public Func<MethodBase, Rule> LateGenerator;
        }

        public List<Rule> Rules = [];
        public List<Type> LocalTypes = [];

        private class MatchData
        {
            public Rule rule;
            public int start, end;
            public Dictionary<int, int> privateMap;
        }

        public bool TryMatchAndReplace(MethodBase method, ref List<CodeInstruction> instructions, out string reason,
            ILGenerator generator = null, bool debug = false)
        {
            var localIndexMap = new Dictionary<int, int>();
            var matches = new List<MatchData>();
            reason = "Success";

            // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
            for (var ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
            {
                Rule rule = Rules[ruleIndex];
                if (rule.LateGenerator != null)
                    rule = rule.LateGenerator(method);
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

                        if (debug)
                            Log.Message($"COMPARE {patternInst} : {inst}");

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
                    };
                    if (debug)
                        Log.Message($"MATCH #{ruleIndex} {matchData.start}-{matchData.end}");

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
                                Log.Message($"COPYMATCH {outInstructions[outInstructions.Count - 1]}");
                        }
                    }

                    instructionIndex = match.end;

                    for (var i = 0; i < match.rule.Output.Length; i++)
                    {
                        CodeInstruction replaceInst = match.rule.Output[i];
                        if (replaceInst.IsStloc())
                        {
                            int localIndex = replaceInst.LocalIndex();
                            if (localIndexMap.TryGetValue(localIndex, out int substituteIndex))
                            {
                            }
                            else if (match.privateMap.TryGetValue(localIndex, out substituteIndex))
                            {
                            }
                            else if (LocalTypes != null && localIndex < LocalTypes.Count && generator != null)
                            {
                                substituteIndex = generator.DeclareLocal(LocalTypes[localIndex]).LocalIndex;
                                localIndexMap.Add(localIndex, substituteIndex);
                            }
                            else
                            {
                                reason = $"Replacement pattern uses unknown local index #{localIndex}";
                                return false;
                            }

                            outInstructions.Add(CodeInstruction.StoreLocal(substituteIndex));
                        }
                        else if (replaceInst.IsLdloc())
                        {
                            int localIndex = replaceInst.LocalIndex();
                            if (localIndexMap.TryGetValue(localIndex, out int substituteIndex))
                            {
                            }
                            else if (match.privateMap.TryGetValue(localIndex, out substituteIndex))
                            {
                            }
                            else if (LocalTypes != null && localIndex < LocalTypes.Count && generator != null)
                            {
                                substituteIndex = generator.DeclareLocal(LocalTypes[localIndex]).LocalIndex;
                                localIndexMap.Add(localIndex, substituteIndex);
                            }
                            else
                            {
                                reason = $"Replacement pattern uses unknown local index #{localIndex}";
                                return false;
                            }

                            outInstructions.Add(CodeInstruction.LoadLocal(substituteIndex));
                        }
                        else
                            outInstructions.Add(replaceInst);

                        if (i == 0 && match.rule.Mode == OutputMode.Replace)
                        {
                            outInstructions[outInstructions.Count - 1].labels = instructions[match.start].labels;
                        }

                        if (debug)
                            Log.Message($"EMIT {outInstructions[outInstructions.Count - 1]}");
                    }

                    if (match.rule.Mode == OutputMode.InsertBefore)
                    {
                        for (int i = match.start; i <= match.end; i++)
                        {
                            outInstructions.Add(instructions[i]);
                            if (debug)
                                Log.Message($"COPYMATCH {outInstructions[outInstructions.Count - 1]}");
                        }
                    }

                }
                else
                {
                    outInstructions.Add(instructions[instructionIndex]);
                    if (debug)
                        Log.Message($"COPY {outInstructions[outInstructions.Count - 1]}");

                }
            }

            // Everything succeeded, now safe to change ref instructions
            instructions = outInstructions;
            return true;
        }

        public void MatchAndReplace(MethodBase method, ref List<CodeInstruction> instructionsList,
            ILGenerator generator = null, [CallerMemberName] string methodName = null, bool debug = false)
        {
            if (!TryMatchAndReplace(method, ref instructionsList, out string reason, generator, debug))
                Log.Error($"{methodName ?? "<Unknown>"}: {reason}");
        }

        /// <summary>
        /// This creates a rule that replaces all calls of a given method with calls of a given other method.
        /// The new method's parameters must start with all parameters of the old method, in order. After that
        /// a parameter called __instance will match against the instance of the function being transpiled, and
        /// other parameters will match against parameters of the function being transpiled by name.
        /// </summary>
        /// <param name="oldMethod"></param>
        /// <param name="newMethod"></param>
        /// <param name="minMatches"></param>
        /// <returns></returns>
        public static Rule RedirectMethodRule(MethodInfo oldMethod, MethodInfo newMethod, int minMatches = 1)
        {
            return new Rule
            {
                LateGenerator = method =>
                {

                    var opcode = oldMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;

                    ParameterInfo[] oldParameters = oldMethod.GetParameters();
                    int oldParameterCount = oldParameters.Length + (oldMethod.IsStatic ? 0 : 1);

                    ParameterInfo[] newParameters = newMethod.GetParameters();
                    int newParameterCount = newParameters.Length + (newMethod.IsStatic ? 0 : 1);

                    List<CodeInstruction> pattern = new();
                    List<CodeInstruction> output = new();

                    pattern.Add(new CodeInstruction(opcode, oldMethod));

                    if (newParameterCount > oldParameterCount)
                    {
                        for (int i = oldParameterCount; i < newParameterCount; i++)
                        {
                            string parameterName = newParameters[i].Name;

                            if (!oldMethod.IsStatic && parameterName == "__instance")
                                output.Add(CodeInstruction.LoadArgument(0));
                            else
                            {
                                int index = method.GetParameters().FirstIndexOf(p => p.Name == parameterName) + (method.IsStatic ? 0 : 1);
                                if (index < 0)
                                    index = method.GetParameters().FirstIndexOf(p => p.ParameterType == newParameters[i].ParameterType) + (method.IsStatic ? 0 : 1);
                                if (index < 0)
                                {
                                    throw new InvalidOperationException(
                                        $"Couldn't find parameter named '{parameterName}' of type {newParameters[i].ParameterType.FullName}");
                                }

                                output.Add(CodeInstruction.LoadArgument(index));
                            }
                        }
                    }

                    output.Add(new CodeInstruction(opcode, newMethod));

                    var rule = new Rule()
                    {
                        Min = minMatches,
                        Max = 0,
                        Mode = OutputMode.Replace,
                        Pattern = pattern.ToArray(),
                        Output = output.ToArray(),
                    };

                    return rule;
                }
            };
        }
    }
}
