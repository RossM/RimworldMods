namespace Yobai;

public sealed class NightVisits(Map map) : MapComponent(map)
{
    private const int CheckIntervalTicks = 250;
    private const float VisitMtbHours = 4f;

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % CheckIntervalTicks != 0)
            return;

        foreach (Building_Bed bed in map.listerBuildings.AllBuildingsColonistOfClass<Building_Bed>())
            bed.GetComp<CompBedSharing>()?.RemoveDepartedSleepers();

        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned.ToList())
        {
            if (CanLeaveBedForNightVisit(pawn)
                && Rand.MTBEventOccurs(VisitMtbHours, GenDate.TicksPerHour, CheckIntervalTicks))
                TryStartNightVisit(pawn);
        }
    }

    internal static bool TryStartNightVisit(Pawn pawn)
    {
        if (!CanLeaveBedForNightVisit(pawn)
            || BedSelectionHelpers.ChooseNightVisitSleepJob(pawn) is not { } visit)
            return false;

        pawn.jobs.StartJob(visit, JobCondition.InterruptOptional,
            preToilReservationsCanFail: true);
        return pawn.CurJob == visit;
    }

    private static bool CanLeaveBedForNightVisit(Pawn pawn)
    {
        if (!BedSharingHelpers.CanChooseSharedBed(pawn) || pawn.Awake() || pawn.CurJob.playerForced
            || pawn.CurJobDef != JobDefOf.LayDown
            || pawn.CurrentBed() is not { Medical: false } bed)
            return false;

        IEnumerable<Pawn> bedmates = bed.GetComp<CompBedSharing>()?.GetBedmates() ?? bed.CurOccupants;
        return !bedmates.Any(other => other != pawn);
    }
}
