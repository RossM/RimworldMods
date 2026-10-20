namespace Yobai;

public sealed class CompBedSharing : ThingComp
{
    public bool NightVisitsEnabled = true;
    private List<TemporarySleepingSlot> temporarySleepingSlots = [];

    internal bool HasTemporaryAssignments
    {
        get
        {
            RemoveDepartedSleepers();
            return temporarySleepingSlots.Count > 0;
        }
    }

    internal bool TryGetSleepingSlot(Pawn pawn, out int slot)
    {
        RemoveDepartedSleepers();
        TemporarySleepingSlot? assignment = temporarySleepingSlots.Find(assignment => assignment.Pawn == pawn);
        slot = assignment?.Slot ?? -1;
        return assignment != null;
    }

    internal IEnumerable<Pawn> GetBedmates()
    {
        return GetSleepingPlaces().Select(place => place.Pawn).Distinct();
    }

    internal bool IsSleepingPlaceAvailable(Pawn pawn, IntVec3 position)
    {
        return GetSleepingPlaces().All(place => place.Pawn == pawn || place.Position != position);
    }

    private IEnumerable<(Pawn Pawn, IntVec3 Position)> GetSleepingPlaces()
    {
        RemoveDepartedSleepers();
        Building_Bed bed = (Building_Bed)parent;
        foreach (Pawn occupant in bed.CurOccupants)
            yield return (occupant, occupant.Position);
        foreach (TemporarySleepingSlot assignment in temporarySleepingSlots)
        {
            if (assignment.Pawn is { } pawn)
                yield return (pawn, bed.GetSleepingSlotPos(assignment.Slot));
        }

        // Vanilla reservations retain an arriving owner's place without duplicating that booking.
        // A temporary assignment replaces the owner's permanent sleeping position for this stay.
        foreach (ReservationManager.Reservation reservation in bed.Map.reservationManager.ReservationsReadOnly)
        {
            Pawn owner = reservation.Claimant;
            if (reservation.Target.Thing == bed && reservation.Job.targetA.Thing == bed
                && typeof(JobDriver_LayDown).IsAssignableFrom(reservation.Job.def.driverClass)
                && bed.IsOwner(owner, out int? slot) && slot is int assignedSlot
                && !temporarySleepingSlots.Any(assignment => assignment.Pawn == owner))
                yield return (owner, bed.GetSleepingSlotPos(assignedSlot));
        }
    }

    internal bool CanAssignSleepingSlot(Pawn pawn, int slot)
    {
        RemoveDepartedSleepers();
        Building_Bed bed = (Building_Bed)parent;
        if (slot < 0 || slot >= bed.SleepingSlotsCount)
            return false;

        if (temporarySleepingSlots.Any(assignment => assignment.Pawn == pawn)
            || !IsSleepingPlaceAvailable(pawn, bed.GetSleepingSlotPos(slot)))
            return false;

        return BedSharingHelpers.HasCompatibleBedmates(pawn, bed);
    }

    internal TemporarySleepingSlot? TryAssignSleepingSlot(Pawn pawn, int slot)
    {
        if (!CanAssignSleepingSlot(pawn, slot))
            return null;

        TemporarySleepingSlot assignment = new(pawn, slot);
        temporarySleepingSlots.Add(assignment);
        return assignment;
    }

    internal void RemoveAssignment(TemporarySleepingSlot assignment)
    {
        temporarySleepingSlots.Remove(assignment);
    }

    internal void RemoveDepartedSleepers()
    {
        Building_Bed bed = (Building_Bed)parent;
        temporarySleepingSlots.RemoveAll(assignment => !assignment.IsInUse(bed));
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        if (Scribe.mode == LoadSaveMode.Saving)
            RemoveDepartedSleepers();
        Scribe_Values.Look(ref NightVisitsEnabled, "Yobai_NightVisits", true);
        Scribe_Collections.Look(ref temporarySleepingSlots, "Yobai_TemporarySleepingSlots",
            LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            temporarySleepingSlots ??= [];
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        Building_Bed bed = (Building_Bed)parent;
        if (!BedSharingHelpers.IsBedEligibleForNightVisits(bed)
            || !bed.OwnersForReading.Any(owner => PolyculeHelpers.GetBedSharingPartners(owner)
                .Any(partner => partner.ownership?.OwnedBed != bed)))
            yield break;

        yield return new Command_Toggle
        {
            defaultLabel = "Yobai_NightVisits".Translate(),
            defaultDesc = "Yobai_NightVisitsDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/ForColonists"),
            isActive = () => NightVisitsEnabled,
            toggleAction = () => NightVisitsEnabled = !NightVisitsEnabled
        };
    }
}
