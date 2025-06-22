using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Mono.Cecil.Cil;
using Verse;
using System.Reflection.Emit;
using RimWorld;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace XylRacesCore
{
    public class InstructionMatcher
    {
        public class Rule
        {
            public int Min = 1, Max = 1;
            public bool PreserveOriginal = false;
            public bool KeepLocals = true;
            public bool Chained = false;
            public CodeInstruction[] Match;
            public CodeInstruction[] Replace;
        }

        public List<Rule> Rules = new();
        public List<Type> LocalTypes = new();

        class MatchData
        {
            public Rule rule;
            public int start, end;
            public Dictionary<int, int> privateMap;
        }

        public bool MatchAndReplace(ref List<CodeInstruction> instructions, out string reason, ILGenerator generator = null, bool debug = false)
        {
            var localIndexMap = new Dictionary<int, int>();
            var matches = new List<MatchData>();
            reason = "Success";

            // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
            for (var ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
            {
                Rule rule = Rules[ruleIndex];
                int matchCount = 0;

                for (int instructionIndex = rule.Chained && matches.Count > 0 ? matches[matches.Count - 1].end + 1 : 0;
                     instructionIndex <= instructions.Count - rule.Match.Length;
                     instructionIndex++)
                {
                    bool isMatch = true;
                    var tempLocalIndexMap = new Dictionary<int, int>();

                    for (int matchIndex = 0; matchIndex < rule.Match.Length; matchIndex++)
                    {
                        var inst = instructions[instructionIndex + matchIndex];
                        var matchInst = rule.Match[matchIndex];

                        if (debug)
                            Log.Message(string.Format("COMPARE {0} : {1}", matchInst, inst));

                        // For a load or store, map the local indexes in the pattern to the actual local indexes used
                        // in the function
                        if (matchInst.IsStloc())
                        {
                            isMatch = inst.IsStloc();
                            if (!isMatch)
                                break;

                            int localIndex = matchInst.LocalIndex();
                            int targetIndex = inst.LocalIndex();

                            if (localIndexMap.TryGetValue(localIndex, out int substituteIndex))
                                isMatch = targetIndex == substituteIndex;
                            else if (tempLocalIndexMap.TryGetValue(localIndex, out substituteIndex))
                                isMatch = targetIndex == substituteIndex;
                            else
                                tempLocalIndexMap.Add(localIndex, targetIndex);
                        }
                        else if (matchInst.opcode.Value == OpCodes.Ldloca.Value ||
                                 matchInst.opcode.Value == OpCodes.Ldloca_S.Value)
                        {
                            isMatch = inst.opcode == matchInst.opcode;
                            if (!isMatch)
                                break;

                            throw new NotSupportedException();
                        }
                        else if (matchInst.IsLdloc())
                        {
                            isMatch = inst.IsLdloc() && 
                                      inst.opcode.Value != OpCodes.Ldloca.Value &&
                                      inst.opcode.Value != OpCodes.Ldloca_S.Value;
                            if (!isMatch)
                                break;

                            int localIndex = matchInst.LocalIndex();

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
                        else if (matchInst.opcode.Value == OpCodes.Call.Value)
                        {
                            isMatch = (inst.opcode.Value == OpCodes.Call.Value ||
                                       inst.opcode.Value == OpCodes.Callvirt.Value) &&
                                      inst.operand.Equals(matchInst.operand);
                        }
                        else if (matchInst.operand == null)
                        {
                            isMatch = inst.opcode.Value == matchInst.opcode.Value && inst.operand == null;
                        }
                        else
                            isMatch = inst.Is(matchInst.opcode, matchInst.operand);

                        if (!isMatch)
                            break;
                    }

                    if (!isMatch)
                        continue;

                    var matchData = new MatchData()
                    {
                        rule = rule,
                        start = instructionIndex,
                        end = instructionIndex + rule.Match.Length - 1,
                        privateMap = tempLocalIndexMap,
                    };
                    if (debug)
                        Log.Message(string.Format("MATCH #{0} {1}-{2}", ruleIndex, matchData.start, matchData.end));

                    matches.Add(matchData);
                    if (rule.KeepLocals)
                        localIndexMap.AddRange(tempLocalIndexMap);
                    matchCount++;
                    if (rule.Max > 0 && matchCount >= rule.Max)
                        break;
                }

                if (matchCount < rule.Min)
                {
                    reason = string.Format("Not enough matches found for substitution #{0}", ruleIndex);
                    return false;
                }
            }

            var sortedMatches = matches.OrderBy(m => m.start).ToList();
            for (int i = 0; i < sortedMatches.Count - 1; i++)
            {
                if (sortedMatches[i].end >= sortedMatches[i + 1].start)
                {
                    reason = "Overlapping matches";
                    return false;
                }
            }

            var declaredLocals = new List<LocalBuilder>(LocalTypes.Count);

            // Make the substitutions
            var outInstructions = new List<CodeInstruction>();
            for (int instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var match = sortedMatches.FirstOrDefault(r => r.start == instructionIndex);

                if (match?.rule.Replace != null)
                {
                    if (match.rule.PreserveOriginal)
                    {
                        for (int i = match.start; i <= match.end; i++)
                            outInstructions.Add(instructions[i]);
                    }

                    instructionIndex = match.end;

                    for (int replacementIndex = 0; replacementIndex < match.rule.Replace.Length; replacementIndex++)
                    {
                        var replaceInst = match.rule.Replace[replacementIndex];

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
                                reason = string.Format("Replacement pattern uses unknown local index #{0}", localIndex);
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
                                reason = string.Format("Replacement pattern uses unknown local index #{0}", localIndex);
                                return false;
                            }

                            outInstructions.Add(CodeInstruction.LoadLocal(substituteIndex));
                        }
                        else
                            outInstructions.Add(replaceInst);

                        if (debug)
                            Log.Message(string.Format("EMIT {0}", outInstructions[outInstructions.Count - 1]));

                    }
                }
                else
                {
                    outInstructions.Add(instructions[instructionIndex]);
                    if (debug)
                        Log.Message(string.Format("COPY {0}", outInstructions[outInstructions.Count - 1]));

                }
            }

            // Everything succeeded, now safe to change ref instructions
            instructions = outInstructions;
            return true;
        }
    }
}
