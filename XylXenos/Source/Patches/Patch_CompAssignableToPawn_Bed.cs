namespace XylXenos.Patches;

[Patch(typeof(CompAssignableToPawn_Bed))]
public static class Patch_CompAssignableToPawn_Bed
{
    [Feature("TODO")]
    [Postfix]
    [Target(nameof(CompAssignableToPawn_Bed.AssigningCandidates))]
    public static void AssigningCandidates_Postfix(Thing ___parent, ref IEnumerable<Pawn> __result)
    {
        ThingDef def = ___parent.def;
        if (!def.HasModExtension<DefModExtension_Bed_GeneRestricted>())
            return;

        __result = __result.Where(p => RestUtility.CanUseBedEver(p, def));
    }
}
