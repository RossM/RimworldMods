namespace Yobai;

internal sealed record NightVisit(Pawn Partner, SleepingPlace Place)
{
    public Job? TryMakeVisitJob(Pawn visitor)
    {
        if (!Place.CanAssignForNightVisit(visitor))
            return null;
        return BedSharingJobHelpers.TryMakeLovinJob(visitor, Partner, Place);
    }
}
