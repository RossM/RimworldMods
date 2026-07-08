namespace XylIdeos;

[HarmonyPatch(typeof(IdeoFoundation))]
public static class Patch_IdeoFoundation
{
    [Feature(Features.ApparelRequirementsOverrideNudity)]
    [HarmonyPrefix]
    [HarmonyPatch("FinalizeIdeo")]
    public static bool FinalizeIdeo_Prefix(Ideo ideo)
    {
        // Change: Only remove conflicting apparel precepts when nudity is required

        var nudityPrecepts = ideo.PreceptsListForReading.Where(precept => precept.def.prefersNudity).ToList();

        if (nudityPrecepts.Count == 0)
            return false;

        List<Precept> preceptsListForReading = ideo.PreceptsListForReading;
        for (int num = preceptsListForReading.Count - 1; num >= 0; num--)
        {
            if (preceptsListForReading[num] is Precept_Apparel preceptApparel &&
                nudityPrecepts.Any(precept => !preceptApparel.CompatibleWith(precept)))
            {
                ideo.RemovePrecept(preceptsListForReading[num]);
            }
        }

        return false;
    }
}
