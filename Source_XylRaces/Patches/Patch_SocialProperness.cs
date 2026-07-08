namespace XylXenos.Patches;

[HarmonyPatch(typeof(SocialProperness))]
public static class Patch_SocialProperness
{
    [Feature(typeof(GeneComp_Hyperlactation))]
    [InfixPostfix(typeof(GridsUtility), nameof(GridsUtility.IsInPrisonCell))]
    [InfixPatch(nameof(SocialProperness.IsSociallyProper), [typeof(Thing), typeof(Pawn), typeof(bool), typeof(bool)])]
    public static void IsInPrisonCell_Postfix(Thing t, ref bool __result)
    {
        if (PatchHelpers.HyperlactatingPrisonerInRoomCanProduce(t.GetRoom(), t.def))
            __result = false;
    }
}
