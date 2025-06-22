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

namespace XylRacesCore
{
    public class InstructionMatcher
    {
        public class Rule
        {
            public int Min = 1, Max = 1;
            public CodeInstruction[] Match;
            public CodeInstruction[] Replace;
        }

        public List<Rule> Rules = new();

        class MatchData
        {
            public Rule rule;
            public int start, end;
        }

        public bool MatchAndReplace(ref List<CodeInstruction> instructions, out string reason, bool debug = false)
        {
            var localIndexMap = new Dictionary<int, int>();
            var matches = new List<MatchData>();
            reason = "Success";

            // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
            for (var ruleIndex = 0; ruleIndex < Rules.Count; ruleIndex++)
            {
                Rule rule = Rules[ruleIndex];
                int matchCount = 0;

                for (int instructionIndex = 0;
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

                            if (localIndexMap.TryGetValue(matchInst.LocalIndex(), out int localIndex))
                                isMatch = inst.LocalIndex() == localIndex;
                            else if (tempLocalIndexMap.TryGetValue(matchInst.LocalIndex(), out localIndex))
                                isMatch = inst.LocalIndex() == localIndex;
                            else
                                tempLocalIndexMap.Add(matchInst.LocalIndex(), inst.LocalIndex());
                        }
                        else if (matchInst.IsLdloc())
                        {
                            isMatch = inst.IsLdloc();
                            if (!isMatch)
                                break;

                            if (localIndexMap.TryGetValue(matchInst.LocalIndex(), out int localIndex))
                                isMatch = inst.LocalIndex() == localIndex;
                            else if (tempLocalIndexMap.TryGetValue(matchInst.LocalIndex(), out localIndex))
                                isMatch = inst.LocalIndex() == localIndex;
                            else
                                tempLocalIndexMap.Add(matchInst.LocalIndex(), inst.LocalIndex());
                        }
                        // For convenience, let call also match callvirt. Nobody wants to worry about
                        // the difference when writing patterns.
                        else if (matchInst.opcode.Value == System.Reflection.Emit.OpCodes.Call.Value)
                        {
                            isMatch = (inst.opcode.Value == System.Reflection.Emit.OpCodes.Call.Value ||
                                       inst.opcode.Value == System.Reflection.Emit.OpCodes.Callvirt.Value) &&
                                      inst.operand.Equals(matchInst.operand);
                        }
                        else
                            isMatch = inst.Is(matchInst.opcode, matchInst.operand);

                        if (!isMatch)
                            break;
                    }

                    if (!isMatch)
                        continue;

                    matches.Add(new MatchData()
                    {
                        rule = rule,
                        start = instructionIndex,
                        end = instructionIndex + rule.Match.Length - 1,
                    });
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

            // Make the substitutions
            var outInstructions = new List<CodeInstruction>();
            for (int instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var match = sortedMatches.FirstOrDefault(r => r.start == instructionIndex);

                if (match?.rule.Replace != null)
                {
                    instructionIndex = match.end;

                    for (int replacementIndex = 0; replacementIndex < match.rule.Replace.Length; replacementIndex++)
                    {
                        var replaceInst = match.rule.Replace[replacementIndex];
                        if (replaceInst.IsStloc())
                        {
                            // Make sure that the local was mapped. If not this is a problem with the pattern
                            if (!localIndexMap.TryGetValue(replaceInst.LocalIndex(), out int localIndex))
                            {
                                reason = string.Format("Replacement pattern uses local index #{0} but it wasn't used in a match pattern", replaceInst.LocalIndex());
                                return false;
                            }
                            outInstructions.Add(CodeInstruction.StoreLocal(localIndex));
                        }
                        else if (replaceInst.IsLdloc())
                        {
                            // Make sure that the local was mapped. If not this is a problem with the pattern
                            if (!localIndexMap.TryGetValue(replaceInst.LocalIndex(), out int localIndex))
                            {
                                reason = string.Format("Replacement pattern uses local index #{0} but it wasn't used in a match pattern", replaceInst.LocalIndex());
                                return false;
                            }
                            outInstructions.Add(CodeInstruction.LoadLocal(localIndex));
                        }
                        else
                            outInstructions.Add(replaceInst);
                    }
                }
                else
                {
                    outInstructions.Add(instructions[instructionIndex]);
                }
            }

            // Everything succeeded, now safe to change ref instructions
            instructions = outInstructions;
            return true;
        }
    }
}
