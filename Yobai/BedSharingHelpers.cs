using Verse.AI.Group;

namespace Yobai;

internal static class BedSharingHelpers
{
    public static bool IsBedEligibleForSharing(Building_Bed bed)
    {
        return bed.Spawned && bed.Faction == Faction.OfPlayer && !bed.Medical && !bed.ForPrisoners
            && bed.def.building.bed_humanlike && !bed.ForHumanBabies
            && bed.def != ThingDefOf.DeathrestCasket;
    }

    public static bool CanShareBedsNow(Pawn pawn)
    {
        return pawn.Spawned && pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike
            && !pawn.Dead && !pawn.Downed && !pawn.Drafted && !pawn.InMentalState && !pawn.IsPrisoner
            && !pawn.Deathresting && pawn.needs?.rest != null && pawn.health.capacities.CanBeAwake
            && !HealthAIUtility.ShouldSeekMedicalRest(pawn);
    }

    public static bool CanChooseSharedBed(Pawn pawn)
    {
        if (!CanShareBedsNow(pawn) || pawn.IsWildMan() || pawn.roping?.IsRoped == true)
            return false;

        // Caravan duties can allow rest while requiring the pawn to stay out of beds.
        Lord? lord = pawn.GetLord();
        return lord?.CurLordToil?.AllowRestingInBed != false;
    }

    public static bool IsBedEligibleForNightVisits(Building_Bed bed)
    {
        return IsBedEligibleForSharing(bed) && bed.SleepingSlotsCount > 1 && bed.AnyUnownedSleepingSlot;
    }

    public static bool IsBedInAssignedPolyculeBedroom(Pawn pawn, Building_Bed bed)
    {
        Building_Bed? ownedBed = pawn.ownership?.OwnedBed;
        Room? room = ownedBed?.GetRoom();
        if (room == null || room.PsychologicallyOutdoors || bed.GetRoom() != room
            || room.Role != RoomRoleDefOf.Bedroom)
            return false;

        HashSet<Pawn> members = PolyculeHelpers.GetPolycule(pawn);
        return members.Count > 1 && bed.OwnersForReading.Any(members.Contains);
    }

    public static bool CanSleepInSharedBed(Pawn pawn, Building_Bed bed)
    {
        return CanShareBedsNow(pawn) && IsBedEligibleForSharing(bed) && bed.Map == pawn.Map
            && HasCompatibleBedmates(pawn, bed);
    }

    public static bool HasCompatibleBedmates(Pawn pawn, Building_Bed bed)
    {
        IEnumerable<Pawn> bedmates = bed.GetComp<CompBedSharing>()?.GetBedmates() ?? bed.CurOccupants;
        return bedmates.All(bedmate => CanShareBedWith(pawn, bedmate, bed));
    }

    public static bool CanShareBedWith(Pawn pawn, Pawn bedmate, Building_Bed bed)
    {
        if (pawn == bedmate || PolyculeHelpers.AreBedSharingPartners(pawn, bedmate))
            return true;

        // Bedroom companions need not be romantic partners, even under spouse-only precepts.
        // The relationship graph still uses only ideoligion-permitted romantic relationships.
        return IsBedInAssignedPolyculeBedroom(pawn, bed)
            && PolyculeHelpers.GetPolycule(pawn).Contains(bedmate);
    }

    public static bool CanUseTemporarySleepingSlot(Pawn pawn, Building_Bed? bed)
    {
        return bed != null && CanSleepInSharedBed(pawn, bed)
            && bed.GetComp<CompBedSharing>() is { } assignments
            && assignments.TryGetSleepingSlot(pawn, out int slot)
            && assignments.IsSleepingPlaceAvailable(pawn, bed.GetSleepingSlotPos(slot));
    }
}
