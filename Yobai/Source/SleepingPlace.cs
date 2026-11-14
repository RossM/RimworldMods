namespace Yobai;

internal sealed record SleepingPlace
{
    public Pawn? AssignedPawn => ActiveAssignment?.Pawn;

    public SleepingAssignments.ActiveAssignment? ActiveAssignment => Bed.Map?.SleepingAssignments.GetAssignment(this);

    internal SleepingPlace(Building_Bed bed, int slot)
    {
        ArgumentNullException.ThrowIfNull(bed);
        if (slot < 0 || slot >= bed.SleepingSlotsCount)
            throw new ArgumentOutOfRangeException(nameof(slot));
        Bed = bed;
        Slot = slot;
    }

    public Building_Bed Bed { get; }
    public int Slot { get; }

    public bool CanAssignForNightVisit(Pawn pawn)
    {
        Pawn? assignee = AssignedPawn;
        return (assignee == null || assignee == pawn) && CanAssignTemporary(pawn);
    }

    public bool CanAssignTemporary(Pawn pawn)
    {
        if (!IsAvailableFor(pawn))
            return false;
        if (!BedSharingHelpers.IsValidBedForWithTemporaryAssignment(Bed, Slot, pawn, pawn, true, guestStatus: pawn.GuestStatus))
            return false;

        return pawn.TemporarySleepingAssignment?.Place.Bed != Bed && pawn.CanSleepInSharedBed(Bed);
    }

    public void AssignTemporary(Pawn pawn)
    {
        if (!CanAssignTemporary(pawn))
            throw new InvalidOperationException("CanAssignTemporary should have already been checked");

        pawn.TemporarySleepingAssignment?.Place.Bed.CompBedSharing.RemoveAssignment(pawn);
        Bed.CompBedSharing.AddAssignment(pawn, Slot);
    }

    public bool BeingUsedBy(Pawn pawn)
    {
        if (!pawn.IsOnMap(Bed.Map!))
            return false;
        IntVec3 position = Bed.GetSleepingSlotPos(Slot);
        if (pawn.GetPosture().InBed() && pawn.Position == position)
            return true;

        Job? job = pawn.CurJob;
        if ((pawn.jobs?.curDriver is not JobDriver_LayDown || job?.targetA.Thing != Bed) &&
            (pawn.jobs?.curDriver is not JobDriver_Lovin || job?.targetB.Thing != Bed))
            return false;

        // Pre-toil validation runs after the job is accepted but before GotoBed starts its path.
        return pawn.jobs.curDriver.CurToilIndex < 0 || pawn.Position == position ||
               (pawn.pather!.Moving && pawn.pather.Destination.Cell == position);
    }

    public bool IsAvailableFor(Pawn pawn)
    {
        Pawn? occupant = Bed.GetCurOccupant(Slot);
        if (occupant != null && occupant != pawn)
            return false;
        SleepingAssignments.ActiveAssignment? assignment = ActiveAssignment;
        return assignment == null || assignment.Pawn == pawn || !assignment.IsInUse;
    }

    public bool BookableBy(Pawn pawn)
    {
        return CanAssignTemporary(pawn) && pawn.CanReserve(Bed, Bed.SleepingSlotsCount, 0);
    }
}
