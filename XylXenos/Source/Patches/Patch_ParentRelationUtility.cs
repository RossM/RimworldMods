namespace XylXenos.Patches;

[Patch(typeof(ParentRelationUtility))]
public static class Patch_ParentRelationUtility
{
    [Feature(nameof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(DirectPawnRelation), nameof(DirectPawnRelation.otherPawn), MemberType.Getter)]
    [Target("GetParent")]
    public static void GetParent_DirectPawnRelation_otherPawn_Postfix(
        DirectPawnRelation __instance,
        [State] out DirectPawnRelation directRelation)
    {
        directRelation = __instance;
    }

    [Feature(nameof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(Pawn), nameof(Pawn.gender), MemberType.Getter)]
    [Target("GetParent")]
    public static void GetParent_Pawn_gender_Postfix(ref Gender __result, [State] in DirectPawnRelation directRelation)
    {
        __result = directRelation.OriginalGender;
    }

    [Feature(nameof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(VirtualPawnRelation), nameof(VirtualPawnRelation.record), MemberType.Getter)]
    [Target("GetVirtualParent")]
    public static void GetVirtualParent_DirectPawnRelation_otherPawn_Postfix(
        VirtualPawnRelation __instance,
        [State] out VirtualPawnRelation virtualRelation)
    {
        virtualRelation = __instance;
    }

    [Feature(nameof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(RelationshipRecord), nameof(RelationshipRecord.Gender), MemberType.Getter)]
    [Target("GetVirtualParent")]
    public static void GetVirtualParent_Pawn_gender_Postfix(ref Gender __result, [State] in VirtualPawnRelation virtualRelation)
    {
        __result = virtualRelation.OriginalGender;
    }

    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    [Target(nameof(ParentRelationUtility.SetFather))]
    public static void SetFather_AddDirectRelation_Postfix(Pawn ___pawn, PawnRelationDef def, Pawn otherPawn)
    {
        if (def != PawnRelationDefOf.Parent)
            return;

        ___pawn.relations.GetDirectRelation(def, otherPawn)?.OriginalGender = Gender.Male;
    }

    [Feature(nameof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(Pawn), nameof(Pawn.gender), MemberType.Getter)]
    [Target(nameof(ParentRelationUtility.SetFather))]
    public static void SetFather_Pawn_gender_Postfix(ref Gender __result)
    {
        __result = Gender.Male;
    }

    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.AddDirectRelation))]
    [Target(nameof(ParentRelationUtility.SetMother))]
    public static void SetMother_AddDirectRelation_Postfix(Pawn ___pawn, PawnRelationDef def, Pawn otherPawn)
    {
        if (def != PawnRelationDefOf.Parent)
            return;

        ___pawn.relations.GetDirectRelation(def, otherPawn)?.OriginalGender = Gender.Female;
    }


    [Feature(nameof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(Pawn), nameof(Pawn.gender), MemberType.Getter)]
    [Target(nameof(ParentRelationUtility.SetMother))]
    public static void SetMother_Pawn_gender_Postfix(ref Gender __result)
    {
        __result = Gender.Female;
    }
}
