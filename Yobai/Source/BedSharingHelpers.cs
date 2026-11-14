using Verse.AI.Group;

namespace Yobai;

internal static class BedSharingHelpers
{
    // This is used to enable checking what result IsValidBedFor() would give if a temporary slot was assigned, without actually
    // needing to set up a temporary slot and undo it afterwards.
    [ThreadStatic] internal static int? candidateSlotForIsValidBedFor;

    extension(Building_Bed bed)
    {
        public bool IsEligibleForBedSharing =>
            bed.IsNormalHumanlikeBed &&
            bed.Faction == Faction.OfPlayer;

        private bool IsNormalHumanlikeBed =>
            bed is { Spawned: true, Medical: false, ForPrisoners: false, ForHumanBabies: false, def.building.bed_humanlike: true } &&
            bed.def != ThingDefOf.DeathrestCasket;

        public bool IsEligibleForNightVisits => bed is { IsEligibleForBedSharing: true, SleepingSlotsCount: > 1 };

        public bool InBedroomOf(Pawn pawn)
        {
            Room? room = pawn.ownership.OwnedBed?.GetRoom();
            return room != null && bed.GetRoom() == room;
        }

        public SleepingPlace GetSleepingPlace(int slot)
        {
            return new SleepingPlace(bed, slot);
        }

        public Pawn? GetAssignedPawn(int slot) => bed.Map?.GetComponent<SleepingAssignments>()!.GetAssignedPawn(bed.GetSleepingPlace(slot));
        public bool HasOccupant(Pawn value) => bed.AllAssignedPawns.Contains(value);
        public IEnumerable<Pawn> AllAssignedPawns => bed.GetComp<CompBedSharing>()?.AllAssignedPawns ?? bed.CurOccupants;
    }

    extension(Pawn pawn)
    {
        public bool CanShareBedsNow =>
            pawn is
            {
                Spawned: true, RaceProps.Humanlike: true, Dead: false, Downed: false, Drafted: false, InMentalState: false,
                IsPrisoner: false, Deathresting: false, needs.rest: not null, health.capacities.CanBeAwake: true,
            } && pawn.Faction == Faction.OfPlayer && !HealthAIUtility.ShouldSeekMedicalRest(pawn) && !pawn.IsWildMan() &&
            pawn.roping?.IsRoped != true &&
            // Caravan duties can allow rest while requiring the pawn to stay out of beds.
            pawn.GetLord()?.CurLordToil?.AllowRestingInBed != false;

        public bool CanSleepInSharedBed(Building_Bed bed)
        {
            return pawn.CanShareBedsNow && bed.IsEligibleForBedSharing && bed.Map == pawn.Map
                   && AllBedmatesWillingToShareBed(pawn, bed);
        }

        internal bool IsOnMap(Map map)
        {
            return pawn is { Spawned: true, Dead: false } && pawn.Map == map;
        }

        internal SleepingAssignments.ActiveAssignment? ActiveSleepingAssignment =>
            pawn.Map?.GetComponent<SleepingAssignments>()!.GetActiveAssignments().FirstOrDefault(assignment => assignment.Pawn == pawn);

        internal SleepingAssignments.ActiveAssignment? TemporarySleepingAssignment =>
            pawn.Map?.GetComponent<SleepingAssignments>()!.GetActiveAssignments().FirstOrDefault(assignment => assignment.Pawn == pawn && assignment.IsTemporary);

        public Building_Bed? ActiveBed => pawn.ActiveSleepingAssignment?.Place.Bed;
    }

    public static bool AllBedmatesWillingToShareBed(Pawn pawn, Building_Bed bed)
    {
        HashSet<Pawn> loveCluster = [.. pawn.GetLoveCluster()];
        return bed.AllAssignedPawns.All(bedmate =>
            pawn == bedmate || BedUtility.WillingToShareBed(pawn, bedmate) || 
            (bed.InBedroomOf(pawn) && bed.OwnersForReading.Any(loveCluster.Contains) && loveCluster.Contains(bedmate)));
    }

    public static bool CanUseTemporarySleepingSlot(Pawn pawn, Building_Bed? bed)
    {
        return bed != null && pawn.CanSleepInSharedBed(bed)
                           && bed.GetComp<CompBedSharing>() is { } comp
                           && comp.TryGetSleepingSlot(pawn, out int slot)
                           && comp.IsSleepingPlaceAvailable(pawn, slot);
    }

    public static bool AreLovePartnersWillingToShareBed(Pawn first, Pawn second)
    {
        return first != second && !first.Dead && !second.Dead
               && LovePartnerRelationUtility.LovePartnerRelationExists(first, second)
               && BedUtility.WillingToShareBed(first, second);
    }

    public static IEnumerable<Pawn> GetLovePartnersWillingToShareBed(Pawn pawn)
    {
        return pawn.relations.DirectRelations.Where(relation => AreLovePartnersWillingToShareBed(pawn, relation.otherPawn))
            .Select(relation => relation.otherPawn);
    }

    public static bool IsValidBedForWithTemporaryAssignment(
        Building_Bed bed,
        int slot,
        Pawn sleeper,
        Pawn traveler,
        bool checkSocialProperness,
        GuestStatus? guestStatus = null)
    {
        candidateSlotForIsValidBedFor = slot;
        try
        {
            return RestUtility.IsValidBedFor(bed, sleeper, traveler, checkSocialProperness, guestStatus: guestStatus);
        }
        finally
        {
            candidateSlotForIsValidBedFor = null;
        }
    }
}
