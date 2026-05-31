namespace XylXenos.Patches;

[HarmonyPatch(typeof(Hediff_Pregnant))]
public static class Patch_Hediff_Pregnant
{
    [Feature(Config.Feature.Parthenogenesis)]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Hediff_Pregnant.PostAdd))]
    public static void PostAdd_Postfix(ref Hediff_Pregnant __instance)
    {
        if (__instance.Mother != null || __instance.Father != null)
            return;

        RimWorld.GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(null, __instance.pawn, out var success);
        if (success)
        {
            __instance.SetParents(__instance.pawn, null, inheritedGeneSet);
        }
    }
}
