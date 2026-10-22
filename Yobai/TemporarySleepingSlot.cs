namespace Yobai;

internal sealed class TemporarySleepingSlot : IExposable
{
    public Pawn? Pawn => pawn;
    public int Slot => slot;
    private Pawn? pawn;
    private int slot;

    [Unsaved] private readonly int tickCreated = -1;

    [UsedImplicitly]
    public TemporarySleepingSlot() { }

    public TemporarySleepingSlot(Pawn pawn, int slot)
    {
        this.pawn = pawn;
        this.slot = slot;
        this.tickCreated = Find.TickManager.TicksGame;
    }

    public bool IsInUse(Building_Bed bed)
    {
        if (!BedSharingHelpers.IsBedEligibleForSharing(bed)
            || pawn == null || pawn.Dead || !pawn.Spawned || pawn.Map != bed.Map)
            return false;
        if (slot < 0 || slot >= bed.SleepingSlotsCount)
            return false;

        // If this reservation was created this tick, we are still in the process of assigning it, so don't remove it.
        if (tickCreated == Find.TickManager.TicksGame)
            return true;

        // Reservations retain a place during travel; physical presence retains it between jobs.
        return pawn.HasReserved(bed)
               || (pawn.GetPosture().InBed() && pawn.Position == bed.GetSleepingSlotPos(slot));
    }

    public void ExposeData()
    {
        Scribe_References.Look(ref pawn, "pawn");
        Scribe_Values.Look(ref slot, "slot");
    }
}
