namespace Yobai;

internal static class BedSelectionHelpers
{
    public static Building_Bed? FindPolyculeBed(Pawn pawn)
    {
        List<Building_Bed> beds =
        [
            .. GetPolyculeBedroomBeds(pawn).Where(bed => HasAvailablePolyculeBedroomSlot(pawn, bed)),
        ];
        beds.Shuffle();

        foreach (Building_Bed bed in beds.OrderByDescending(bed => bed.GetComp<CompBedSharing>()!.GetBedmates().Any(bedmate => PolyculeHelpers.AreLovePartnersWillingToShareBed(pawn, bedmate)) || bed.GetComp<CompBedSharing>()!.FreeSlotsExcluding(pawn) >= 2))
        {
            if (TryMakePolyculeBedroomSleepJob(pawn, bed))
                return bed;
        }

        return null;
    }

    public static bool TryMakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        foreach (int slot in GetAvailablePolyculeBedroomSlots(pawn, bed))
        {
            SleepingPlace place = bed.GetSleepingPlace(slot);

            if (!place.BookableBy(pawn))
                continue;
            if (!place.TryAssignTemporary(pawn))
                continue;

            return true;
        }

        return false;
    }

    private static IEnumerable<Building_Bed> GetPolyculeBedroomBeds(Pawn pawn)
    {
        return pawn.ownership.OwnedBed?.GetRoom()?.ContainedBeds ?? [];
    }

    private static bool HasAvailablePolyculeBedroomSlot(Pawn pawn, Building_Bed bed)
    {
        if (!ValidBed(pawn, bed) || bed.GetComp<CompBedSharing>() is not { } comp)
            return false;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        HashSet<Pawn> members = [.. pawn.GetLoveCluster()];
        if (bed.IsOwner(pawn, out int? assignedSlot) && assignedSlot is int ownedSlot)
            return true;

        foreach (int slot in slots)
        {
            Pawn? owner = bed.OwnersForReading.ElementAtOrDefault(slot);
            if (owner != null && !members.Contains(owner))
                continue;
            if (comp.CanAssignSleepingSlot(pawn, slot))
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
            if (comp.CanAssignSleepingSlot(pawn, slot))
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
