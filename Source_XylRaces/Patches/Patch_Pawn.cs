using HarmonyLib;
using JetBrains.Annotations;
using System;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn))]
    public static class Patch_Pawn
    {
        public static Lazy<bool> Enabled = new(Config.GeneWithModExtensionExists<GeneDefExtension_Pawn>);

        [Feature(nameof(GeneDefExtension_Pawn)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
        public static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            if (Enabled.Value == false)
                return;

            using (new ProfileBlock())
            {
                foreach (var extension in __instance.ActiveGeneDefExtensionsOfType<GeneDefExtension_Pawn>())
                   __result *= extension.bodySizeFactor;
            }
        }

        [Feature(nameof(GeneDefExtension_Pawn)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        public static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            if (Enabled.Value == false)
                return;

            using (new ProfileBlock())
            {
                foreach (var extension in __instance.ActiveGeneDefExtensionsOfType<GeneDefExtension_Pawn>())
                   __result *= extension.healthScaleFactor;
            }
        }
    }
}
