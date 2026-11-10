namespace Yobai;

[UsedImplicitly]
public sealed class CompBedSharing : ThingComp
{
    internal Building_Bed Bed => (Building_Bed)parent;

    public bool nightVisitsEnabled = true;
    private List<TemporarySleepingSlot> temporarySleepingSlots = [];

    internal IReadOnlyList<TemporarySleepingSlot> TemporaryAssignments
    {
        get
        {
            RemoveDepartedSleepers();
            return temporarySleepingSlots;
        }
    }

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
        if (SleepingPlace.alwaysAllowSlot is { } alwaysAllowSlot)
        {
            slot = alwaysAllowSlot;
            return true;
        }

        SleepingPlace? assignment = SleepingAssignments.GetTemporaryAssignment(pawn);
        slot = assignment?.Bed == Bed ? assignment.Slot : -1;
        return slot >= 0;
    }

    internal IEnumerable<Pawn> GetBedmates()
    {
        return SleepingAssignments.GetBedmates(Bed);
    }

    internal bool IsSleepingPlaceAvailable(Pawn pawn, IntVec3 position)
    {
        if (SleepingPlace.alwaysAllowSlot is not null)
            return true;

        for (int slot = 0; slot < Bed.SleepingSlotsCount; slot++)
        {
            if (Bed.GetSleepingSlotPos(slot) == position)
                return IsSleepingPlaceAvailable(pawn, slot);
        }

        return false;
    }

    internal bool IsSleepingPlaceAvailable(Pawn pawn, int slot) => Bed.GetSleepingPlace(slot).IsAvailableFor(pawn);

    internal bool CanAssignSleepingSlot(Pawn pawn, int slot)
    {
        if (slot < 0 || slot >= Bed.SleepingSlotsCount)
            return false;
        return Bed.GetSleepingPlace(slot).CanAssignTemporary(pawn);
    }

    internal void AddAssignment(TemporarySleepingSlot assignment)
    {
        temporarySleepingSlots.Add(assignment);
    }

    internal void RemoveAssignment(TemporarySleepingSlot assignment)
    {
        temporarySleepingSlots.Remove(assignment);
    }

    internal void RemoveDepartedSleepers()
    {
        temporarySleepingSlots.RemoveAll(assignment => !IsActive(assignment));
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
        if (!Bed.IsEligibleForNightVisits
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

    public int FreeSlotsExcluding(Pawn pawn)
    {
        return Bed.SleepingSlotsCount - GetBedmates().Count(p => p != pawn);
    }

    private bool IsActive(TemporarySleepingSlot assignment)
    {
        Building_Bed bed = Bed;
        if (!bed.IsEligibleForBedSharing
            || assignment.Pawn is not { } pawn || !pawn.IsOnMap(bed.Map!)
            || assignment.Slot < 0 || assignment.Slot >= bed.SleepingSlotsCount)
            return false;

        return assignment.TickCreated == Find.TickManager.TicksGame || bed.GetSleepingPlace(assignment.Slot).BeingUsedBy(pawn);
    }
}
