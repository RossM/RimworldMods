namespace Yobai;

[UsedImplicitly]
public sealed class CompBedSharing : ThingComp
{
    private Building_Bed Bed => (Building_Bed)parent;

    public bool nightVisitsEnabled = true;
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

    internal bool IsSleepingPlaceAvailable(Pawn pawn, int slot) => IsSleepingPlaceAvailable(pawn, Bed.GetSleepingSlotPos(slot));

    private IEnumerable<(Pawn Pawn, IntVec3 Position)> GetSleepingPlaces()
    {
        RemoveDepartedSleepers();
        foreach (Pawn occupant in Bed.CurOccupants)
            yield return (occupant, occupant.Position);
        foreach (TemporarySleepingSlot assignment in temporarySleepingSlots)
        {
            if (assignment.Pawn is { } pawn)
                yield return (pawn, Bed.GetSleepingSlotPos(assignment.Slot));
        }

        // Vanilla reservations retain an arriving owner's place without duplicating that booking.
        // A temporary assignment replaces the owner's permanent sleeping position for this stay.
        foreach (ReservationManager.Reservation reservation in Bed.Map.reservationManager.ReservationsReadOnly)
        {
            Pawn owner = reservation.Claimant;
            if (reservation.Target.Thing == Bed && reservation.Job.targetA.Thing == Bed
                                                && typeof(JobDriver_LayDown).IsAssignableFrom(reservation.Job.def.driverClass)
                                                && Bed.IsOwner(owner, out int? slot) && slot is int assignedSlot
                                                && !temporarySleepingSlots.Any(assignment => assignment.Pawn == owner))
                yield return (owner, Bed.GetSleepingSlotPos(assignedSlot));
        }
    }

    internal bool CanAssignSleepingSlot(Pawn pawn, int slot)
    {
        RemoveDepartedSleepers();
        if (slot < 0 || slot >= Bed.SleepingSlotsCount)
            return false;

        if (temporarySleepingSlots.Any(assignment => assignment.Pawn == pawn)
            || !IsSleepingPlaceAvailable(pawn, slot))
            return false;

        return BedSharingHelpers.AllBedmatesWillingToShareBed(pawn, Bed);
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
        Scribe_Values.Look(ref nightVisitsEnabled, "Yobai_NightVisits", true);
        Scribe_Collections.Look(ref temporarySleepingSlots, "Yobai_TemporarySleepingSlots",
            LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
            temporarySleepingSlots ??= [];
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (!BedSharingHelpers.IsBedEligibleForNightVisits(Bed)
            || !Bed.OwnersForReading.Any(owner => PolyculeHelpers.GetLovePartnersWillingToShareBed(owner)
                .Any(partner => partner.ownership.OwnedBed != Bed)))
            yield break;

        yield return new Command_Toggle
        {
            defaultLabel = "Yobai_NightVisits".Translate(),
            defaultDesc = "Yobai_NightVisitsDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/ForColonists"),
            isActive = () => nightVisitsEnabled,
            toggleAction = () => nightVisitsEnabled = !nightVisitsEnabled,
        };
    }
}
