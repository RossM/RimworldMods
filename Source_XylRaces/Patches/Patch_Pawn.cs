using System;
using HarmonyLib;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn))]
    public static class Patch_Pawn
    {
        public static bool Enabled => enabled.Value;
        public static Lazy<bool> enabled = new(Config.GeneWithModExtensionExists<GeneDefExtension_Pawn>);

        // Note: This patch is performance-sensitive
        [Feature(typeof(GeneDefExtension_Pawn))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
        public static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            if (!Enabled)
                return;

            __result *= GeneHelpers.GetBodySizeFactor(__instance);
        }

        [Feature(typeof(GeneDefExtension_Pawn))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        public static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            if (!Enabled)
                return;

            __result *= GeneHelpers.GetHealthScaleFactor(__instance);
        }

        [Feature(typeof(Psycast))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
        public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
        {
            __result |= __instance.HasActivePsycastGene();
        }
    }
}
