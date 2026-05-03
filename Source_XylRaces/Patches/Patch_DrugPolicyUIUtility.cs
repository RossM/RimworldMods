using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TranspilerUtil;
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
                // We can't just patch TryGetChemicalDependencyGene directly because it returns Gene_ChemicalDependency, and we
                // need a function that returns just Gene.
                InstructionMatcher.RedirectMethodRule(AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.TryGetChemicalDependencyGene)), 
                    AccessTools.Method(typeof(Patch_DrugPolicyUIUtility), nameof(TryGetChemicalDependencyGene_Wrapper)))
            }
        };

        [Feature(nameof(Defs.XylDrugSensitive)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(DrugPolicyUIUtility.DoAssignDrugPolicyButtons))]
        public static IEnumerable<CodeInstruction> DoAssignDrugPolicyButtons_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_DoAssignDrugPolicyButtons.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static bool TryGetChemicalDependencyGene_Wrapper(Pawn pawn, out Gene gene)
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
