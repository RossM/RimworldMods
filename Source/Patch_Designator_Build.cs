using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;


namespace XylRacesCore
{
    [HarmonyPatch(typeof(Designator_Build))]
    public static class Patch_Designator_Build
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Designator_Build.Visible), MethodType.Getter)]
        static void Visible_Postfix(Designator_Build __instance, ref bool __result)
        {
            if (DebugSettings.godMode)
                return;
            if (__result == false)
                return;

            BuildableDef def = __instance.PlacingDef;
            var extension = def.GetModExtension<BuildableDefExtension>();
            if (extension == null) 
                return;

            __result = extension.ValidateBuildable(__instance.Map);
        }
    }
}
