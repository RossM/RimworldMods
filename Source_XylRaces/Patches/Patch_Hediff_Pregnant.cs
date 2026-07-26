namespace XylXenos.Patches;

[HarmonyPatch(typeof(Hediff_Pregnant))]
public static class Patch_Hediff_Pregnant
{
    [Feature(nameof(Config.Feature.Parthenogenesis))]
    [Postfix]
    [Target(nameof(Hediff_Pregnant.PostAdd))]
    public static void PostAdd_Postfix(Hediff_Pregnant __instance)
    {
        if (__instance.Mother != null || __instance.Father != null)
            return;

        GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(null, __instance.pawn, out var success);
        if (success)
        {
            __instance.SetParents(__instance.pawn, null, inheritedGeneSet);
        }
    }
}
