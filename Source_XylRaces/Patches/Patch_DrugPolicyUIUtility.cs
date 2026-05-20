using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(DrugPolicyUIUtility))]
    public static class Patch_DrugPolicyUIUtility
    {
        private static readonly InstructionMatcher.Rule Rule_TryGetChemicalDependencyGene
            = InstructionMatcher.MakeRedirectRule(PawnUtility.TryGetChemicalDependencyGene, TryGetChemicalDependencyGene_Wrapper);

        [Feature(typeof(GeneDefExtension_Chemicals))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(DrugPolicyUIUtility.DoAssignDrugPolicyButtons))]
        public static IEnumerable<CodeInstruction> DoAssignDrugPolicyButtons_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    // We can't just patch TryGetChemicalDependencyGene directly because it returns Gene_ChemicalDependency, and we
                    // need a function that returns just Gene.
                    Rule_TryGetChemicalDependencyGene
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static bool TryGetChemicalDependencyGene_Wrapper(Pawn pawn, out Gene gene)
        {
            if (PawnUtility.TryGetChemicalDependencyGene(pawn, out var chemicalDependency))
            {
                gene = chemicalDependency;
                return true;
            }

            gene = pawn.genes?.GenesListForReading.FirstOrDefault(g =>
                g.def.GetModExtension<GeneDefExtension_Chemicals>()?.showInDrugPolicies == true);
            return gene != null;
        }
    }
}
