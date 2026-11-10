namespace Yobai;

internal static class BedSelectionHelpers
{
    public static Building_Bed? FindPolyculeBed(Pawn pawn)
    {
        List<Building_Bed> beds =
        [
            .. GetPolyculeBedroomBeds(pawn).Where(bed => HasAvailablePolyculeBedroomSlot(pawn, bed)),
        ];

        if (beds.Count == 0)
            return null;

        int bedsWithTwoFreeSlots = beds.Count(bed => bed.GetComp<CompBedSharing>()!.FreeSlotsExcluding(pawn) >= 2);
        
        if (!beds.TryRandomElementByWeight(WeightSelector, out var selected))
            selected = beds.RandomElement()!;
        MakePolyculeBedroomSleepJob(pawn, selected);
        return selected;

        float WeightSelector(Building_Bed bed)
        {
            int partnerCount = bed.GetComp<CompBedSharing>()!.GetBedmates()
                .Count(bedmate => BedSharingHelpers.AreLovePartnersWillingToShareBed(pawn, bedmate));
            if (partnerCount > 0)
                //return partnerCount;
                return 1f;
            if (bed.GetComp<CompBedSharing>()!.FreeSlotsExcluding(pawn) >= 2)
                //return 1f / bedsWithTwoFreeSlots;
                return 1f;
            return 0f;
        }
    }

    internal static bool TryMakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        if (!HasAvailablePolyculeBedroomSlot(pawn, bed))
            return false;
        MakePolyculeBedroomSleepJob(pawn, bed);
        return true;
    }

    public static void MakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        var slot = GetAvailablePolyculeBedroomSlots(pawn, bed).First();
        SleepingPlace place = bed.GetSleepingPlace(slot);
        place.AssignTemporary(pawn);
    }

    private static IEnumerable<Building_Bed> GetPolyculeBedroomBeds(Pawn pawn)
    {
        return pawn.ownership.OwnedBed?.GetRoom()?.ContainedBeds ?? [];
    }

    internal static bool HasAvailablePolyculeBedroomSlot(Pawn pawn, Building_Bed bed)
    {
        if (!ValidBed(pawn, bed) || bed.GetComp<CompBedSharing>() is not { } comp)
            return false;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        HashSet<Pawn> members = [.. pawn.GetLoveCluster()];
        if (bed.IsOwner(pawn, out int? assignedSlot) && assignedSlot is int ownedSlot && bed.GetSleepingPlace(ownedSlot) is SleepingPlace place2 && place2.BookableBy(pawn))
            return true;

        foreach (int slot in slots)
        {
            Pawn? owner = bed.OwnersForReading.ElementAtOrDefault(slot);
            if (owner != null && !members.Contains(owner))
                continue;
            if (bed.GetSleepingPlace(slot) is SleepingPlace place && place.BookableBy(pawn))
                return true;
        }

        return false;
    }

    private static IEnumerable<int> GetAvailablePolyculeBedroomSlots(Pawn pawn, Building_Bed bed)
    {
        if (!ValidBed(pawn, bed) || bed.GetComp<CompBedSharing>() is not { } comp)
            yield break;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        HashSet<Pawn> members = [.. pawn.GetLoveCluster()];
        if (bed.IsOwner(pawn, out int? assignedSlot) && assignedSlot is int ownedSlot)
            slots = slots.Prepend(ownedSlot).Distinct();
        foreach (int slot in slots)
        {
            Pawn? owner = bed.OwnersForReading.ElementAtOrDefault(slot);
            if (owner != null && !members.Contains(owner))
                continue;
            if (bed.GetSleepingPlace(slot) is SleepingPlace place && place.BookableBy(pawn))
                yield return slot;
        }
    }

    private static bool ValidBed(Pawn pawn, Building_Bed bed)
    {
        HashSet<Pawn> members = [.. pawn.GetLoveCluster()];
        return pawn.CanShareBedsNow
               && pawn.CanSleepInSharedBed(bed)
               && bed.InBedroomOf(pawn) && 
               bed.OwnersForReading.Any(members.Contains);
    }
}
