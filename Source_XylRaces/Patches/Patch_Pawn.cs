using HarmonyLib;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Pawn))]
    public static class Patch_Pawn
    {
        // Note: This patch is performance-sensitive
        [Feature(nameof(GeneDefExt.bodySizeFactor))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
        public static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            __result *= __instance.GeneSet()?.bodySizeFactor ?? 1f;
        }

        [Feature(nameof(GeneDefExt.healthScaleFactor))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        public static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            __result *= __instance.GeneSet()?.healthScaleFactor ?? 1f;
        }

        [Feature(typeof(Psycast))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.HasPsylink), MethodType.Getter)]
        public static void HasPsylink_Postfix(Pawn __instance, ref bool __result)
        {
            __result |= __instance.HasActivePsycastGene();
        }

        [Feature(typeof(GeneSet))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.Discard))]
        public static void Discard_Postfix(Pawn __instance)
        {
            NotificationManager.Instance.Notify(NotificationEvent.PostDiscard, __instance);
        }

        [Feature(typeof(GeneSet))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn.PostMake))]
        public static void PostMake_Postfix(Pawn __instance)
        {
            NotificationManager.Instance.Notify(NotificationEvent.PostPostMake, __instance);
        }
    }
}
