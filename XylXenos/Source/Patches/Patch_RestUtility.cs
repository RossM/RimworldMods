namespace XylXenos.Patches;

[Patch(typeof(RestUtility))]
public static class Patch_RestUtility
{
    [Feature("TODO")]
    [Postfix]
    [Target(nameof(RestUtility.CanUseBedEver))]
    public static void CanUseBedEver_Postfix(Pawn p, ThingDef bedDef, ref bool __result)
    {
        if (!__result)
            return;

        if (bedDef.GetModExtension<DefModExtension_Bed_GeneRestricted>() is not { } geneRestriction)
            return;

        if (geneRestriction.requiredGenesAny is { Count: > 0 })
            __result &= geneRestriction.requiredGenesAny.Any(p.HasActiveGene);
    }
}
