using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
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
            public Func<MethodBase, List<CodeInstruction>, Rule> LateGenerator;
            public Type[] LocalTypes;
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
                    rule = rule.LateGenerator(method, instructions);
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
                            Debug.Log($"COMPARE {patternInst} : {inst}");

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
                            int localIndex = replaceInst.LocalIndex();
                            if (localIndexMap.TryGetValue(localIndex, out int substituteIndex))
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

        public void MatchAndReplace(MethodBase method, ref List<CodeInstruction> instructionsList,
            ILGenerator generator = null, [CallerMemberName] string methodName = null, bool debug = false)
        {
            if (!TryMatchAndReplace(method, ref instructionsList, out string reason, generator, debug))
                Log.Error($"{methodName ?? "<Unknown>"}: {reason}");
        }

        /// <summary>
        /// This creates a rule that replaces all calls of a given method with calls of a given other method. The
        /// new method's parameters will be filled with the values of the old method's parameters that have the
        /// same name. If the old method doesn't have a parameter with that name, the parameters of the method
        /// containing the call being modified are checked, and used if they match.
        ///
        /// You can also use __instance to match the instance the method was invoked on, and __caller to match
        /// the instance the calling method was invoked on.
        ///
        /// If there isn't a parameter with a matching name, this will fall back to trying to match based
        /// on parameter type, but this may result in less optimal code generation, and will give a warning.
        /// </summary>
        /// <param name="oldMember"></param>
        /// <param name="newMember"></param>
        /// <param name="minMatches"></param>
        /// <returns></returns>
        public static Rule MakeRedirectRule(MemberInfo oldMember, MethodInfo newMember, int minMatches = 1)
        {
            return new Rule
            {
                LateGenerator = (caller, _) => RedirectRule_Core(caller, oldMember, newMember, minMatches)
            };
        }

        public static Rule MakeRedirectRule(string oldMemberName, MethodInfo newMember, int minMatches = 1)
        {
            return new Rule
            {
                LateGenerator = (caller, instructions) =>
                {
                    MemberInfo oldMember = FindMemberInfo(oldMemberName, instructions);

                    return RedirectRule_Core(caller, oldMember, newMember, minMatches);
                }
            };
        }

        private static MemberInfo FindMemberInfo(string oldMemberName, List<CodeInstruction> instructions)
        {
            MemberInfo oldMember = (MemberInfo)instructions.First(NameMatches).operand;
            Debug.Log($"oldMemberName={oldMemberName} oldMember={oldMember}");
            return oldMember;

            bool NameMatches(CodeInstruction instruction)
            {
                if (instruction.opcode.Value == OpCodes.Call.Value || instruction.opcode.Value == OpCodes.Callvirt.Value)
                    return ((MethodBase)instruction.operand).Name == oldMemberName;
                if (instruction.opcode.Value == OpCodes.Ldfld.Value || instruction.opcode.Value == OpCodes.Ldsfld.Value)
                    return ((FieldInfo)instruction.operand).Name == oldMemberName;
                return false;
            }
        }

        private static Rule RedirectRule_Core(MethodBase caller, MemberInfo callee, MemberInfo replacement,
            int minMatches)
        {
            (Type[] callerParameterTypes, string[] callerParameterNames) = GetParameterTypesAndNames(caller, "__caller");
            (Type[] calleeParameterTypes, string[] calleeParameterNames) = GetParameterTypesAndNames(callee, "__instance");
            (Type[] replacementParameterTypes, string[] replacementParameterNames) = GetParameterTypesAndNames(replacement, "__instance");

            List<CodeInstruction> pattern = new();
            List<CodeInstruction> output = new();
            List<Type> localTypes = new();

            pattern.Add(new CodeInstruction(OpcodeFor(callee), callee));

            // Instructions which are already on the stack in the right order don't need to be saved and restored
            int firstNonMatchingParameter = 0;
            while (firstNonMatchingParameter < replacementParameterNames.Length &&
                   firstNonMatchingParameter < calleeParameterNames.Length &&
                   replacementParameterNames[firstNonMatchingParameter] == calleeParameterNames[firstNonMatchingParameter])
            {
                firstNonMatchingParameter++;
            }

            // Save all remaining parameters to local. The matcher will handle renumbering the locals to new
            // unused local indexes.
            int[] parameterToLocalIndex = new int[calleeParameterTypes.Length];
            for (int i = calleeParameterTypes.Length - 1; i >= firstNonMatchingParameter; i--)
            {
                parameterToLocalIndex[i] = localTypes.Count;
                localTypes.Add(calleeParameterTypes[i]);
                output.Add(CodeInstruction.StoreLocal(parameterToLocalIndex[i]));
            }

            // Match each parameter of the replacement method
            for (int i = firstNonMatchingParameter; i < replacementParameterNames.Length; i++)
            {
                string replacementParameterName = replacementParameterNames[i];
                Type replacementParameterType = replacementParameterTypes[i];

                int calleeIndex = calleeParameterNames.FirstIndexOf(name => name == replacementParameterName);
                if (calleeIndex >= 0)
                {
                    if (calleeIndex < firstNonMatchingParameter)
                        throw new InvalidOperationException($"Can't reuse parameter named '{replacementParameterName}' of type {replacementParameterType.FullName}");
                    output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[calleeIndex]));
                    continue;
                }

                int callerIndex = callerParameterNames.FirstIndexOf(name => name == replacementParameterName);
                if (callerIndex >= 0)
                {
                    output.Add(CodeInstruction.LoadArgument(callerIndex));
                    continue;
                }

                calleeIndex = calleeParameterTypes.FirstIndexOf(type => type == replacementParameterType);
                if (calleeIndex >= 0)
                {
                    Log.Warning($"RedirectMethodRule on {caller.DeclaringType?.FullName}.{caller.Name} ({callee.Name} -> {replacement.Name}): Matching by type: {replacementParameterType.Name} {replacementParameterName} = {calleeParameterTypes[calleeIndex].Name} {calleeParameterNames[calleeIndex]}");
                    if (calleeIndex < firstNonMatchingParameter)
                        throw new InvalidOperationException($"Can't reuse parameter named '{replacementParameterName}' of type {replacementParameterType.FullName}");
                    output.Add(CodeInstruction.LoadLocal(parameterToLocalIndex[calleeIndex]));
                    continue;
                }

                callerIndex = callerParameterTypes.FirstIndexOf(type => type == replacementParameterType);
                if (callerIndex >= 0)
                {
                    Log.Warning($"RedirectMethodRule on {caller.DeclaringType?.FullName}.{caller.Name} ({callee.Name} -> {replacement.Name}): Matching by type: {replacementParameterType.Name} {replacementParameterName} = caller's {callerParameterTypes[callerIndex].Name} {callerParameterNames[callerIndex]}");
                    output.Add(CodeInstruction.LoadArgument(callerIndex));
                    continue;
                }

                throw new InvalidOperationException(
                    $"Couldn't find parameter named '{replacementParameterName}' of type {replacementParameterType.FullName}");
            }

            output.Add(new CodeInstruction(OpcodeFor(replacement), replacement));

            var rule = new Rule()
            {
                Min = minMatches,
                Max = 0,
                Mode = OutputMode.Replace,
                Pattern = pattern.ToArray(),
                Output = output.ToArray(),
                LocalTypes = localTypes.ToArray(),
            };

            return rule;
        }

        private static (Type[] types, string[] names) GetParameterTypesAndNames(MemberInfo member, string instanceName)
        {
            return member switch
            {
                FieldInfo { IsStatic: true } => (
                    [], 
                    []),
                FieldInfo field => (
                    [field.DeclaringType], 
                    [instanceName]),
                MethodInfo { IsStatic: true} method => (
                    [.. (method?.GetParameters()).Select(p => p.ParameterType)],
                    [.. (method?.GetParameters()).Select(p => p.Name)]),
                MethodInfo method => (
                    [method.DeclaringType, .. (method?.GetParameters()).Select(p => p.ParameterType)],
                    [instanceName, .. (method?.GetParameters()).Select(p => p.Name)]),
                _ => throw new InvalidOperationException()
            };
        }

        private static OpCode OpcodeFor(MemberInfo callee)
        {
            return callee switch
            {
                FieldInfo { IsStatic: true } => OpCodes.Ldsfld,
                FieldInfo => OpCodes.Ldfld,
                MethodBase { IsVirtual: true } => OpCodes.Callvirt,
                MethodBase => OpCodes.Call,
                _ => throw new InvalidOperationException()
            };
        }
    }
}
