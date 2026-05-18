using System;
using HarmonyLib;
using JetBrains.Annotations;
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
        [Feature(nameof(GeneDefExtension_Pawn))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
        public static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            if (!Enabled)
                return;

            foreach (var extension in __instance.ActiveGeneDefExtensionsOfType<GeneDefExtension_Pawn>())
                __result *= extension.bodySizeFactor;
        }

        [Feature(nameof(GeneDefExtension_Pawn))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        public static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            if (!Enabled)
                return;

            foreach (var extension in __instance.ActiveGeneDefExtensionsOfType<GeneDefExtension_Pawn>())
                __result *= extension.healthScaleFactor;
        }

        [Feature(nameof(Psycast))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
        public static bool HasPsylink_Prefix(Pawn __instance, ref bool __result)
        {
            __result = __instance.psychicEntropy?.Psylink != null || __instance.HasActivePsycastGene();
            return false;
        }
    }
}
