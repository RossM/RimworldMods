using HarmonyLib;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Gene))]
    public static class Patch_Gene
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.PostAdd))]
        public static void PostAdd_Postfix(Gene __instance)
        {
            if (__instance is INotificationListener target)
                target.RegisterWith(NotificationManager.Instance);
        }

        // Note: This patch is performance-sensitive
        [Feature(nameof(GeneDefExt.gender))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.Active), MethodType.Getter)]
        public static void Active_Postfix(Gene __instance, ref bool __result)
        {
            if (!__result)
                return;
            if (__instance.def is GeneDefExt ext)
                __result = ext.gender == null || ext.gender == __instance.pawn.gender;
        }

        [Feature(nameof(GeneDefExt.hediffGivers))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.TickInterval))]
        public static void TickInterval_Postfix(Gene __instance, int delta)
        {
            __instance.TickIntervalExt(delta);
        }
    }
}
