using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(ThingDef))]
    public static class Patch_ThingDef
    {
        public static bool Enabled => enabled.Value;
        public static Lazy<bool> enabled = new(Config.GeneOfTypeExists<Flight>);

        [Feature(nameof(Flight))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(ThingDef.SpecialDisplayStats))]
        public static void SpecialDisplayStats_Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
            if (!Enabled)
                return;

            if (__instance.apparel != null)
            {
                bool allowsFlight = Flight.ApparelAllowsFlight(__instance);

                __result = __result.AddItem(
                    new StatDrawEntry(StatCategoryDefOf.Apparel,
                        "XylAllowsFlightLabel".Translate(),
                        allowsFlight ? "Yes".Translate() : "No".Translate(),
                        "XylAllowsFlightDesc".Translate(),
                        2752)
                );
            }
        }
    }
}
