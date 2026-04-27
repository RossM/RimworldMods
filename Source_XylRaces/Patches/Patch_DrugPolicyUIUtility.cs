using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(DrugPolicyUIUtility))]
    public static class Patch_DrugPolicyUIUtility
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly] public static GeneDef XylDrugSensitive;
        }

        private static readonly InstructionMatcher Fixup_DoAssignDrugPolicyButtons = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.Call(() => PawnUtility.TryGetChemicalDependencyGene)
                    ],
                    Output =
                    [
                        CodeInstruction.Call(() => TryGetChemicalAffectingGene),
                    ]
                },
            }
        };

        [Feature(nameof(Defs.XylDrugSensitive)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(DrugPolicyUIUtility.DoAssignDrugPolicyButtons))]
        public static IEnumerable<CodeInstruction> DoAssignDrugPolicyButtons_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DoAssignDrugPolicyButtons.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static bool TryGetChemicalAffectingGene(Pawn pawn, out Gene gene)
        {
            if (PawnUtility.TryGetChemicalDependencyGene(pawn, out var chemicalDependency))
            {
                gene = chemicalDependency;
                return true;
            }

            var drugSensitive = pawn.genes?.GetGene(Defs.XylDrugSensitive);
            gene = drugSensitive;
            return gene != null;
        }
    }
}
