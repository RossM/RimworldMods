using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(HediffComp_Lactating))]
    public static class Patch_HediffComp_Lactating
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(HediffComp_Lactating.TryCharge))]
        public static void Patch_TryCharge(HediffComp_Lactating __instance, ref float desiredChargeAmount)
        {
            if (!Config.FeatureEnabled(Config.Feature.FixLactationBugs))
                return;

            // Fixes a bug where lactation kept consuming food even when full, despite the hediff tooltip saying it doesn't
            desiredChargeAmount = Mathf.Min(desiredChargeAmount, __instance.Props.fullChargeAmount - __instance.Charge);
        }
    }
}
