using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(HealthUtility))]
    public static class Patch_HealthUtility
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly]
            public static StatDef XylHypothermiaProgressionFactor;
            [UsedImplicitly]
            public static StatDef XylMalnutritionProgressionFactor;
        }

        [Feature(nameof(Defs.XylHypothermiaProgressionFactor), nameof(Defs.XylMalnutritionProgressionFactor)),
         HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(HealthUtility.AdjustSeverity))]
        public static void AdjustSeverity_Prefix(Pawn pawn, HediffDef hdDef, ref float sevOffset)
        {
            if (hdDef == HediffDefOf.Hypothermia)
                sevOffset *= pawn.GetStatValue(Defs.XylHypothermiaProgressionFactor);
            if (hdDef == HediffDefOf.Malnutrition)
                sevOffset *= pawn.GetStatValue(Defs.XylMalnutritionProgressionFactor);
        }
    }
}
