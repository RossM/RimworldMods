using Verse.AI.Group;

namespace Yobai;

internal static class BedSharingHelpers
{
    extension(Building_Bed bed)
    {
        public bool IsEligibleForBedSharing =>
            bed.IsNormalHumanlikeBed &&
            bed.Faction == Faction.OfPlayer;

        private bool IsNormalHumanlikeBed =>
            bed is { Spawned: true, Medical: false, ForPrisoners: false, ForHumanBabies: false, def.building.bed_humanlike: true } &&
            bed.def != ThingDefOf.DeathrestCasket;

        public bool IsEligibleForNightVisits => bed is { IsEligibleForBedSharing: true, SleepingSlotsCount: > 1 };

        public bool InPolyculeBedroomFor(Pawn pawn) => bed.InBedroomOf(pawn) && bed.OwnedByPolyculeMemberWith(pawn);

        private bool OwnedByPolyculeMemberWith(Pawn pawn)
        {
            HashSet<Pawn> members = [.. pawn.GetLoveCluster()];
            return members.Count > 1 && bed.OwnersForReading.Any(members.Contains);
        }

        private bool InBedroomOf(Pawn pawn)
        {
            Building_Bed? ownedBed = pawn.ownership.OwnedBed;
            Room? room = ownedBed?.GetRoom();
            if (room == null || bed.GetRoom() != room)
                return false;
            return true;
        }
    }

    extension(Pawn pawn)
    {
        public bool CanShareBedsNow =>
            pawn is
            {
                Spawned: true, RaceProps.Humanlike: true, Dead: false, Downed: false, Drafted: false, InMentalState: false,
                IsPrisoner: false, Deathresting: false, needs.rest: not null, health.capacities.CanBeAwake: true,
            } && pawn.Faction == Faction.OfPlayer && !HealthAIUtility.ShouldSeekMedicalRest(pawn) && !pawn.IsWildMan() && pawn.roping?.IsRoped != true &&
            // Caravan duties can allow rest while requiring the pawn to stay out of beds.
            pawn.GetLord()?.CurLordToil?.AllowRestingInBed != false;

        private bool CanSleepInBed(Building_Bed bed) =>
            bed.Map == pawn.Map
            && AllBedmatesWillingToShareBed(pawn, bed);

        public bool CanSleepInSharedBed(Building_Bed bed)
        {
            return pawn.CanShareBedsNow && bed.IsEligibleForBedSharing && pawn.CanSleepInBed(bed);
        }
    }

    public static bool AllBedmatesWillingToShareBed(Pawn pawn, Building_Bed bed)
    {
        IEnumerable<Pawn> bedmates = bed.GetComp<CompBedSharing>()?.GetBedmates() ?? bed.CurOccupants;
        return bedmates.All(bedmate => WillingToShareBed(pawn, bedmate, bed));
    }

    public static bool WillingToShareBed(Pawn pawn, Pawn bedmate, Building_Bed bed)
    {
        if (pawn == bedmate || BedUtility.WillingToShareBed(pawn, bedmate))
            return true;

        return bed.InPolyculeBedroomFor(pawn) && pawn.GetLoveCluster().Contains(bedmate);
    }

    public static bool CanUseTemporarySleepingSlot(Pawn pawn, Building_Bed? bed)
    {
        return bed != null && pawn.CanSleepInSharedBed(bed)
                           && bed.GetComp<CompBedSharing>() is { } comp
                           && comp.TryGetSleepingSlot(pawn, out int slot)
                           && comp.IsSleepingPlaceAvailable(pawn, slot);
    }
}
