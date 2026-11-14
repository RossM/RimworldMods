namespace Yobai;

[UsedImplicitly]
public sealed class CompBedSharing : ThingComp
{
    private sealed class TemporarySleepingSlot : IExposable
    {
        public Pawn Pawn => pawn;
        public int Slot => slot;

        private Pawn pawn;
        private int slot;

        [UsedImplicitly]
        public TemporarySleepingSlot()
        {
            pawn = null!;
        }

        public TemporarySleepingSlot(Pawn pawn, int slot)
        {
            this.pawn = pawn;
            this.slot = slot;
            TickCreated = Find.TickManager.TicksGame;
        }

        [field: Unsaved]
        public int TickCreated { get; } = -1;

        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Values.Look(ref slot, "slot");
        }
    }

    internal Building_Bed Bed => (Building_Bed)parent;

    public bool nightVisitsEnabled = true;
    private List<TemporarySleepingSlot> temporarySleepingSlots = [];

    internal IEnumerable<(Pawn pawn, int slot)> TemporaryAssignments
    {
        get
        {
            RemoveDepartedSleepers();
            foreach (var temporarySleepingSlot in temporarySleepingSlots)
                yield return (temporarySleepingSlot.Pawn, temporarySleepingSlot.Slot);
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

    internal bool HasSleepingSlot(Pawn pawn)
    {
        return BedSharingHelpers.candidateSlotForIsValidBedFor is not null || SleepingAssignments.GetTemporaryAssignment(pawn)?.Bed == Bed;
    }

    internal bool TryGetSleepingSlot(Pawn pawn, out int slot)
    {
        if (BedSharingHelpers.candidateSlotForIsValidBedFor is { } allowSlot)
        {
            slot = allowSlot;
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
        if (BedSharingHelpers.candidateSlotForIsValidBedFor is not null)
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
            || !Bed.OwnersForReading.Any(owner => BedSharingHelpers.GetLovePartnersWillingToShareBed(owner)
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
        return Bed.IsEligibleForBedSharing && assignment.Pawn.IsOnMap(Bed.Map!) && IsValidAssignment(Bed, assignment) &&
               (assignment.TickCreated == Find.TickManager.TicksGame || Bed.GetSleepingPlace(assignment.Slot).BeingUsedBy(assignment.Pawn));
    }

    private static bool IsValidAssignment(Building_Bed bed, TemporarySleepingSlot assignment) =>
        assignment.Slot >= 0 && assignment.Slot < bed.SleepingSlotsCount;

    public bool RemoveAssignment(Pawn pawn)
    {
        TemporarySleepingSlot? previousAssignment = temporarySleepingSlots.FirstOrDefault(assignment => assignment.Pawn == pawn);
        if (previousAssignment == null)
            return false;
        temporarySleepingSlots.Remove(previousAssignment);
        return true;
    }

    public bool AddAssignment(Pawn pawn, int slot)
    {
        TemporarySleepingSlot? previousAssignment = temporarySleepingSlots.FirstOrDefault(assignment => assignment.Pawn == pawn);
        if (previousAssignment != null)
            return false;
        temporarySleepingSlots.Add(new(pawn, slot));
        return true;
    }
}
