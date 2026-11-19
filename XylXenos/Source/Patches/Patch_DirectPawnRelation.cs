using Waylith.Injection;

namespace XylXenos.Patches;

internal static class DirectPawnRelationExtensions
{
    extension(DirectPawnRelation _)
    {
        [InjectedField]
        public extern ref Gender OriginalGender { get; }
    }
}

[Patch(typeof(DirectPawnRelation))]
public static class Patch_DirectPawnRelation
{
    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Target(nameof(DirectPawnRelation.ExposeData))]
    public static void ExposeData_Postfix(DirectPawnRelation __instance)
    {
        Scribe_Values.Look(ref __instance.OriginalGender, "XylOriginalGender");

        if (Scribe.mode == LoadSaveMode.PostLoadInit && __instance is { OriginalGender: Gender.None, otherPawn: not null })
            __instance.OriginalGender = __instance.otherPawn.gender;
    }

    [Feature(typeof(GeneCompProperties_GenderRatio))]
    [Postfix]
    [Target(memberType: MemberType.Constructor, parameterTypes: [typeof(PawnRelationDef), typeof(Pawn), typeof(int)])]
    public static void Constructor_Postfix(DirectPawnRelation __instance, PawnRelationDef def, Pawn otherPawn, int startTicks)
    {
        __instance.OriginalGender = otherPawn.gender;
    }
}
