namespace Yobai;

internal static class BedSelectionHelpers
{
    public static Building_Bed? FindPolyculeBed(Pawn pawn)
    {
        List<Building_Bed> beds = GetPolyculeBedroomBeds(pawn)
            .Where(bed => GetAvailablePolyculeBedroomSlots(pawn, bed).Any()).ToList();
        beds.Shuffle();

        foreach (Building_Bed bed in beds.OrderByDescending(bed => HasLovePartnerWillingToShareBed(pawn, bed) || FreeSlots(pawn, bed) >= 2))
        {
            if (TryMakePolyculeBedroomSleepJob(pawn, bed))
                return bed;
        }
        return null;
    }

    private static int FreeSlots(Pawn pawn, Building_Bed bed)
    {
        return bed.SleepingSlotsCount - bed.GetComp<CompBedSharing>().GetBedmates().Count(p => p != pawn);
    }

    private static bool HasLovePartnerWillingToShareBed(Pawn pawn, Building_Bed bed)
    {
        return bed.GetComp<CompBedSharing>().GetBedmates().Any(bedmate => PolyculeHelpers.AreLovePartnersWillingToShareBed(pawn, bedmate));
    }

    public static bool TryMakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        foreach (int slot in GetAvailablePolyculeBedroomSlots(pawn, bed))
        {
            SleepingPlace place = new SleepingPlace(bed, slot);

            if (!BedSharingJobHelpers.CanBookSleepingPlace(pawn, place))
                continue;
            if (!SleepingAssignments.TryAssignTemporary(pawn, place))
                continue;

            return true;
        }
        return false;
    }

    private static IEnumerable<Building_Bed> GetPolyculeBedroomBeds(Pawn pawn)
    {
        return pawn.ownership.OwnedBed?.GetRoom()?.ContainedBeds ?? [];
    }

    private static IEnumerable<int> GetAvailablePolyculeBedroomSlots(Pawn pawn, Building_Bed bed)
    {
        if (!BedSharingHelpers.CanChooseSharedBed(pawn)
            || !BedSharingHelpers.CanSleepInSharedBed(pawn, bed)
            || !BedSharingHelpers.IsBedInAssignedPolyculeBedroom(pawn, bed)
            || bed.GetComp<CompBedSharing>() is not { } comp)
            yield break;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        HashSet<Pawn> members = PolyculeHelpers.GetPolycule(pawn);
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
}
