namespace XylXenos.Patches;

[Patch(typeof(Pawn_RelationsTracker))]
public static class Patch_Pawn_RelationsTracker
{
    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Inner(typeof(VirtualPawnRelation), memberType: MemberType.Constructor,
        parameterTypes: [typeof(PawnRelationDef), typeof(RelationshipRecord), typeof(int)])]
    [Target(nameof(Pawn_RelationsTracker.ElevateToVirtualRelation))]
    public static void GetOrCreateRecord_Postfix(DirectPawnRelation relation, VirtualPawnRelation __result)
    {
        __result.OriginalGender = relation.OriginalGender;
    }
}
