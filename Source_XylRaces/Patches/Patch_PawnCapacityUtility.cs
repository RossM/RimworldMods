using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnCapacityUtility))]
    public static class Patch_PawnCapacityUtility

    {
        private static readonly InstructionMatcher.Rule Rule_HediffStage_partEfficiencyOffset = InstructionMatcher.MakeRedirectRule(
            AccessTools.Field(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset)),
            HediffStage_partEfficiencyOffset_Wrapper);

        [Feature(typeof(HediffWithCompsExt))]
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(PawnCapacityUtility.CalculatePartEfficiency))]
        public static IEnumerable<CodeInstruction> CalculatePartEfficiency_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_HediffStage_partEfficiencyOffset
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static float HediffStage_partEfficiencyOffset_Wrapper(HediffStage __instance, HediffSet diffSet)
        {
            var hediff = diffSet.hediffs.FirstOrDefault(hediff => hediff.CurStage == __instance);
            if (hediff is HediffWithCompsExt ext)
                return ext.PartEfficiencyOffset;

            return __instance.partEfficiencyOffset;
        }
    }
}
