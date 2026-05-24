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
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Gene.Active), MethodType.Getter)]
        public static void Active_Postfix(Gene __instance, ref bool __result)
        {
            var activeGender = __instance.def.GetModExtension<GeneDefExtension_GenderLocked>()?.activeGender;
            if (activeGender != null && activeGender != __instance.pawn.gender)
                __result = false;
        }
    }
}
