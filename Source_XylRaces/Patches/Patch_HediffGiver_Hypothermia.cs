using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(HediffGiver_Hypothermia))]
    public static class Patch_HediffGiver_Hypothermia
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly] 
            public static StatDef XylHypothermiaProgressionFactor;
        }

        private static readonly InstructionMatcher Fixup_OnIntervalPassed = new()
        {
            Rules =
            {
                InstructionMatcher.RedirectMethodRule(typeof(HealthUtility), nameof(HealthUtility.AdjustSeverity), typeof(Patch_HediffGiver_Hypothermia), nameof(AdjustSeverity_Hypothermia))
            }
        };

        public static void AdjustSeverity_Hypothermia(Pawn pawn, HediffDef hdDef, float sevOffset)
        {
            sevOffset *= pawn.GetStatValue(Defs.XylHypothermiaProgressionFactor);
            HealthUtility.AdjustSeverity(pawn, hdDef, sevOffset);
        }

        [Feature(nameof(Defs.XylHypothermiaProgressionFactor)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("OnIntervalPassed")]
        public static IEnumerable<CodeInstruction> OnIntervalPassed_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_OnIntervalPassed.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

    }
}
