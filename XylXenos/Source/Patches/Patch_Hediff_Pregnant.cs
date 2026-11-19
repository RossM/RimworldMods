namespace XylXenos.Patches;

[Patch(typeof(Hediff_Pregnant))]
public static class Patch_Hediff_Pregnant
{
    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    [Target(nameof(Hediff_Pregnant.DoBirthSpawn))]
    public static void DoBirthSpawn_Postfix(Pawn ___pawn, PawnRelationDef def, Pawn otherPawn, Pawn mother, Pawn father)
    {
        if (def != PawnRelationDefOf.Parent)
            return;

        if (otherPawn == mother)
            ___pawn.relations.GetDirectRelation(def, otherPawn)?.OriginalGender = Gender.Female;
        else if (otherPawn == father)
            ___pawn.relations.GetDirectRelation(def, otherPawn)?.OriginalGender = Gender.Male;
    }

    [Feature(FeatureNameOf.Parthenogenesis)]
    [Postfix]
    [Target(nameof(Hediff_Pregnant.PostAdd))]
    public static void PostAdd_Postfix(Hediff_Pregnant __instance)
    {
        if (__instance.Mother != null || __instance.Father != null)
            return;

        GeneSet inheritedGeneSet = PregnancyUtility.GetInheritedGeneSet(null, __instance.pawn, out var success);
        if (success)
            __instance.SetParents(__instance.pawn, null, inheritedGeneSet);
    }
}
