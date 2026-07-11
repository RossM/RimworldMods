namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class HediffCompProperties_ForceMentalState : HediffCompProperties
{
    public required MentalStateDef mentalState;
    public bool endMentalStateOnCure = true;

    public HediffCompProperties_ForceMentalState()
    {
        compClass = typeof(HediffComp_ForceMentalState);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        foreach (var error in base.ConfigErrors(parentDef))
            yield return error;

        if (mentalState is null)
            yield return $"{nameof(mentalState)} is null";
    }
}

[PublicAPI]
public class HediffComp_ForceMentalState : HediffComp
{
    public HediffCompProperties_ForceMentalState Props => (HediffCompProperties_ForceMentalState)props;

    public override bool CompShouldRemove
    {
        get
        {
            DebugAssert.NotNull(Pawn);

            return Pawn.mindState.mentalStateHandler.CurStateDef != Props.mentalState;
        }
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        DebugAssert.NotNull(Pawn);

        Pawn.mindState.mentalStateHandler.TryStartMentalState(Props.mentalState, forced: true, forceWake: true, causedByDamage: true);
    }

    public override void CompPostPostRemoved()
    {
        DebugAssert.NotNull(Pawn);

        if (Props.endMentalStateOnCure && Pawn.mindState.mentalStateHandler.CurStateDef == Props.mentalState &&
            !Pawn.mindState.mentalStateHandler.CurState!.causedByMood)
        {
            Pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
        }
    }
}
