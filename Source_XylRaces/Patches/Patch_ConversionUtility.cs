namespace XylXenos.Patches;

[HarmonyPatch(typeof(ConversionUtility))]
public static class Patch_ConversionUtility
{
    [Feature(nameof(DefModExtension_Xenotype.agreeingMemes))]
    [Feature(nameof(DefModExtension_Xenotype.disagreeingMemes))]
    [InnerPostfix(typeof(ConversionUtility), "<ConversionPowerFactor_MemesVsTraits>g__OffsetFromIdeo|1_1")]
    [Target(typeof(ConversionUtility), nameof(ConversionUtility.ConversionPowerFactor_MemesVsTraits))]
    public static void OffsetFromIdeo_Postfix(Pawn pawn, bool invert, StringBuilder sb, Pawn recipient, ref float __result)
    {
        __result += PatchHelpers.ConversionPowerFactor_OffsetFromXenotype(pawn, invert, sb, recipient);
    }
}
