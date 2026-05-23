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
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
        public static bool HasPsylink_Prefix(Pawn __instance, out bool __result)
        {
            __result = __instance.psychicEntropy?.Psylink != null || __instance.HasActivePsycastGene();
            return false;
        }
    }
}
