using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnCapacityUtility))]
    public static class Patch_PawnCapacityUtility

    {
        private static readonly InstructionMatcher Fixup_HediffStage_partEfficiencyOffset = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Field(typeof(HediffStage), nameof(HediffStage.partEfficiencyOffset)),
                    HediffStage_partEfficiencyOffset_Wrapper)
            }
        };

        [Feature(typeof(Hediff_Petrified))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnCapacityUtility.CalculatePartEfficiency))]
        public static IEnumerable<CodeInstruction> CalculatePartEfficiency_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_HediffStage_partEfficiencyOffset.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static float HediffStage_partEfficiencyOffset_Wrapper(HediffStage __instance, HediffSet diffSet)
        {
            var hediff = diffSet.hediffs.FirstOrDefault(hediff => hediff.CurStage == __instance);
            if (hediff is Hediff_Petrified h)
                return h.PartEfficiencyOffset;

            return __instance.partEfficiencyOffset;
        }
    }
}
