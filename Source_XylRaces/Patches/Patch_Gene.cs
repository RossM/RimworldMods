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

        [Feature(typeof(GeneDefExtension_GenderLocked))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Gene.Active), MethodType.Getter)]
        public static bool Active_Prefix(Gene __instance, out bool __result)
        {
            __result = false;

            var activeGender = __instance.def.GetModExtension<GeneDefExtension_GenderLocked>()?.activeGender;
            return activeGender == null || activeGender == __instance.pawn.gender;
        }
    }
}
