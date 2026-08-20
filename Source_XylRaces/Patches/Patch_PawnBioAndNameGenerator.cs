namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnBioAndNameGenerator))]
public static class Patch_PawnBioAndNameGenerator
{
    [Feature(typeof(DefModExtension_Xenotype))]
    [Prefix]
    [Inner(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo")]
    [Target(nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
    public static bool TryGiveSolidBioTo_Prefix(XenotypeDef? xenotype, out bool __result)
    {
        __result = false;
        return Settings.instance.AllowBackerBackstoriesFor(xenotype);
    }
}
