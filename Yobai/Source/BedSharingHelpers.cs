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

        public bool InBedroomOf(Pawn pawn)
        {
            Room? room = pawn.ownership.OwnedBed?.GetRoom();
            return room != null && bed.GetRoom() == room;
        }

        public SleepingPlace GetSleepingPlace(int slot)
        {
            return new SleepingPlace(bed, slot);
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

        public bool CanSleepInSharedBed(Building_Bed bed)
        {
            return pawn.CanShareBedsNow && bed.IsEligibleForBedSharing && bed.Map == pawn.Map
                   && AllBedmatesWillingToShareBed(pawn, bed);
        }
    }

    public static bool AllBedmatesWillingToShareBed(Pawn pawn, Building_Bed bed)
    {
        IEnumerable<Pawn> bedmates = bed.GetComp<CompBedSharing>()?.GetBedmates() ?? bed.CurOccupants;
        HashSet<Pawn> loveCluster = [.. pawn.GetLoveCluster()];
        return bedmates.All(bedmate => pawn == bedmate || BedUtility.WillingToShareBed(pawn, bedmate) ||
                                       (bed.InBedroomOf(pawn) && bed.OwnersForReading.Any(loveCluster.Contains) && loveCluster.Contains(bedmate)));
    }

    public static bool CanUseTemporarySleepingSlot(Pawn pawn, Building_Bed? bed)
    {
        return bed != null && pawn.CanSleepInSharedBed(bed)
                           && bed.GetComp<CompBedSharing>() is { } comp
                           && comp.TryGetSleepingSlot(pawn, out int slot)
                           && comp.IsSleepingPlaceAvailable(pawn, slot);
    }
}
