namespace XylXenos.Patches;

[HarmonyPatch(typeof(IdeoUtility))]
public static class Patch_IdeoUtility
{
    [Feature(typeof(IncidentWorker_WildTribe))]
    [Prefix]
    [Target(nameof(IdeoUtility.CanUseIdeo))]
    public static bool CanUseIdeo_Prefix(FactionDef factionDef, Ideo ideo, out bool __result)
    {
        __result = false;

        XenotypeSet? xenotypeSet = factionDef.xenotypeSet;
        if (xenotypeSet == null)
            return true;

        var precept = (Precept_Xenotype?)ideo.GetPrecept(PreceptDefOf.PreferredXenotype);

        return precept is not { xenotype: { } xenotype } || xenotypeSet.Contains(xenotype);
    }
}
