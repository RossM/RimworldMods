using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(ThingDef))]
    public static class Patch_ThingDef
    {
        [Feature(typeof(Flight))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ThingDef.SpecialDisplayStats))]
        public static void SpecialDisplayStats_Postfix(ThingDef __instance, ref IEnumerable<StatDrawEntry> __result)
        {
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
