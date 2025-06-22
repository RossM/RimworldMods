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
        public class Substitution
        {
            public CodeInstruction[] Match;
            public CodeInstruction[] Replace;
        }

        public List<Substitution> Substitutions = new();

        public bool MatchAndReplace(ref List<CodeInstruction> instructions, out string reason, bool debug = false)
        {
            var localIndexMap = new Dictionary<int, int>();
            var substitutionLocations = new List<int>(Enumerable.Repeat(-1, Substitutions.Count));
            reason = "Success";

            // Check and make sure that all the substitutions apply. Also work out the indexes of all locals.
            for (int substitutionIndex = 0; substitutionIndex < Substitutions.Count; substitutionIndex++)
            {
                var substitution = Substitutions[substitutionIndex];

                for (int instructionIndex = 0; instructionIndex <= instructions.Count - substitution.Match.Length; instructionIndex++)
                {
                    bool matches = true;
                    var tempLocalIndexMap = new Dictionary<int, int>();

                    for (int matchIndex = 0; matchIndex < substitution.Match.Length; matchIndex++)
                    {
                        var inst = instructions[instructionIndex + matchIndex];
                        var matchInst = substitution.Match[matchIndex];

                        if (debug)
                            Log.Message(string.Format("COMPARE {0} : {1}", matchInst, inst));

                        // For a load or store, map the local indexes in the pattern to the actual local indexes used
                        // in the function
                        if (matchInst.IsStloc())
                        {
                            matches = inst.IsStloc();
                            if (!matches)
                                break;

                            if (localIndexMap.TryGetValue(matchInst.LocalIndex(), out int localIndex))
                                matches = inst.LocalIndex() == localIndex;
                            else if (tempLocalIndexMap.TryGetValue(matchInst.LocalIndex(), out localIndex))
                                matches = inst.LocalIndex() == localIndex;
                            else
                                tempLocalIndexMap.Add(matchInst.LocalIndex(), inst.LocalIndex());
                        }
                        else if (matchInst.IsLdloc())
                        {
                            matches = inst.IsLdloc();
                            if (!matches)
                                break;

                            if (localIndexMap.TryGetValue(matchInst.LocalIndex(), out int localIndex))
                                matches = inst.LocalIndex() == localIndex;
                            else if (tempLocalIndexMap.TryGetValue(matchInst.LocalIndex(), out localIndex))
                                matches = inst.LocalIndex() == localIndex;
                            else
                                tempLocalIndexMap.Add(matchInst.LocalIndex(), inst.LocalIndex());
                        }
                        // For convenience, let call also match callvirt. Nobody wants to worry about
                        // the difference when writing patterns.
                        else if (matchInst.opcode.Value == System.Reflection.Emit.OpCodes.Call.Value)
                        {
                            matches = (inst.opcode.Value == System.Reflection.Emit.OpCodes.Call.Value ||
                                       inst.opcode.Value == System.Reflection.Emit.OpCodes.Callvirt.Value) &&
                                      inst.operand.Equals(matchInst.operand);
                        }
                        else
                            matches = inst.Is(matchInst.opcode, matchInst.operand);

                        if (!matches)
                            break;
                    }

                    if (!matches)
                        continue;

                    substitutionLocations[substitutionIndex] = instructionIndex;
                    localIndexMap.AddRange(tempLocalIndexMap);
                    break;
                }
            }

            // Check that everything match successfully
            for (int substitutionLocationIndex = 0; substitutionLocationIndex < substitutionLocations.Count; substitutionLocationIndex++)
            {
                if (substitutionLocations[substitutionLocationIndex] < 0)
                {
                    reason = string.Format("No match found for substitution #{0}", substitutionLocationIndex);
                    return false;
                }
            }

            // Make the substitutions
            var outInstructions = new List<CodeInstruction>();
            for (int instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var matchingSubstitutions = new List<InstructionMatcher.Substitution>();
                for (int substitutionIndex = 0; substitutionIndex < Substitutions.Count; substitutionIndex++)
                {
                    InstructionMatcher.Substitution substitution = Substitutions[substitutionIndex];
                    if (substitution.Replace != null && substitutionLocations[substitutionIndex] == instructionIndex) 
                        matchingSubstitutions.Add(substitution);
                }

                if (matchingSubstitutions.Count > 1)
                {
                    reason = string.Format("Multiple substitutions found at instruction #{0} = {1}", instructionIndex, instructions[instructionIndex]);
                    return false;
                }

                if (matchingSubstitutions.Count == 1)
                {
                    var substitution = matchingSubstitutions[0];

                    instructionIndex += substitution.Match.Length - 1;

                    for (int replacementIndex = 0; replacementIndex < substitution.Replace.Length; replacementIndex++)
                    {
                        var replaceInst = substitution.Replace[replacementIndex];
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
