namespace Yobai;

internal sealed class SleepingPlace
{
    public SleepingPlace(Building_Bed bed, int slot)
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
        Pawn? assignee = SleepingAssignments.GetAssignedPawn(this);
        return (assignee == null || assignee == pawn) && CanAssignTemporary(pawn);
    }

    public bool CanAssignTemporary(Pawn pawn)
    {
        if (Bed.GetComp<CompBedSharing>() == null || !SleepingAssignments.IsAvailable(pawn, this))
            return false;

        return SleepingAssignments.GetTemporaryAssignment(pawn)?.Bed != Bed && pawn.CanSleepInSharedBed(Bed);
    }

    public bool TryAssignTemporary(Pawn pawn)
    {
        if (!CanAssignTemporary(pawn))
            return false;

        SleepingPlace? previous = SleepingAssignments.GetTemporaryAssignment(pawn);
        CompBedSharing? previousComp = previous?.Bed.GetComp<CompBedSharing>();
        TemporarySleepingSlot? previousAssignment
            = previousComp?.TemporaryAssignments.FirstOrDefault(assignment => assignment.Pawn == pawn);
        if (previousAssignment != null)
            previousComp!.RemoveAssignment(previousAssignment);

        CompBedSharing comp = Bed.GetComp<CompBedSharing>();
        TemporarySleepingSlot assignment = new(pawn, Slot);
        comp.AddAssignment(assignment);
        if (RestUtility.IsValidBedFor(Bed, pawn, pawn, true, guestStatus: pawn.GuestStatus))
            return true;

        comp.RemoveAssignment(assignment);
        if (previousAssignment != null)
            previousComp!.AddAssignment(previousAssignment);
        return false;
    }

    public bool BeingUsedBy(Pawn pawn)
    {
        Building_Bed bed = Bed;
        if (!pawn.IsOnMap(bed.Map))
            return false;
        IntVec3 position = Bed.GetSleepingSlotPos(Slot);
        if (pawn.GetPosture().InBed() && pawn.Position == position)
            return true;

        Job? job = pawn.CurJob;
        if ((pawn.jobs?.curDriver is not JobDriver_LayDown || job?.targetA.Thing != Bed) && 
            (pawn.jobs?.curDriver is not JobDriver_Lovin || job?.targetB.Thing != Bed))
            return false;

        // Pre-toil validation runs after the job is accepted but before GotoBed starts its path.
        return pawn.jobs.curDriver.CurToilIndex < 0 || pawn.Position == position
                                                    || (pawn.pather!.Moving && pawn.pather.Destination.Cell == position);
    }
}
