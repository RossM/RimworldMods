namespace XylIdeos;

[HarmonyPatch(typeof(Precept_Apparel))]
public class Patch_Precept_Apparel
{
    private static readonly IReadOnlyCollection<BodyPartGroupDef> ExcludedBodyParts =
        [BodyPartGroupDefOf.Torso, BodyPartGroupDefOf.Legs];

    [Feature(Features.ApparelRequirementsOverrideNudity)]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Precept_Apparel.CompatibleWith))]
    public static bool CompatibleWith_Prefix(Precept_Apparel __instance, Precept other, out bool __result)
    {
        __result = true;

        if (__instance.apparelDef.apparel.countsAsClothingForNudity &&
            __instance.apparelDef.apparel.bodyPartGroups.Intersect(ExcludedBodyParts).Any())
            return true;

        // Change: Apparel requirement precepts ARE compatible with nudity precepts
        if (other.def.prefersNudity &&
            (__instance.TargetGender == Gender.None || __instance.TargetGender == other.def.genderPrefersNudity))
        {
            return false;
        }

        return true;
    }
}
