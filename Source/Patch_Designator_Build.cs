using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Visible), MethodType.Getter)]
    public class Patch_Designator_Build
    {
        [HarmonyPostfix]
        static void Postfix(Designator_Build __instance, ref bool __result)
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
