using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Source_XylIdeoTweaks
{
    [HarmonyPatch(typeof(Precept_Apparel))]
    public class Patch_Precept_Apparel
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Precept_Apparel.CompatibleWith))]
        public static bool CompatibleWith_Prefix(Precept_Apparel __instance, Precept other, ref bool __result)
        {
            // Change: Apparel requirement precepts ARE compatible with nudity precepts
            if (other.def.prefersNudity && (__instance.TargetGender == Gender.None || __instance.TargetGender == other.def.genderPrefersNudity))
            {
                __result = true;
                return false;
            }

            return true;
        }
    }
}
