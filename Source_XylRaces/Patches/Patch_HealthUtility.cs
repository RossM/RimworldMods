using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(HealthUtility))]
    public static class Patch_HealthUtility
    {
        [Feature(nameof(DefOf.XylHypothermiaProgressionFactor), nameof(DefOf.XylMalnutritionProgressionFactor))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(HealthUtility.AdjustSeverity))]
        public static void AdjustSeverity_Prefix(Pawn pawn, HediffDef hdDef, ref float sevOffset)
        {
            if (hdDef == HediffDefOf.Hypothermia)
                sevOffset *= pawn.GetStatValue(DefOf.XylHypothermiaProgressionFactor);
            if (hdDef == HediffDefOf.Malnutrition)
                sevOffset *= pawn.GetStatValue(DefOf.XylMalnutritionProgressionFactor);
        }
    }
}
