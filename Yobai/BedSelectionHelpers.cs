namespace Yobai;

internal static class BedSelectionHelpers
{
    public static Job? ChooseBedtimeSleepJob(Pawn pawn)
    {
        List<Building_Bed> beds = GetPolyculeBedroomBeds(pawn)
            .Where(bed => GetAvailablePolyculeBedroomSlots(pawn, bed).Any()).ToList();
        beds.Shuffle();

        foreach (Building_Bed bed in beds.OrderByDescending(bed => HasBedSharingPartner(pawn, bed) || FreeSlots(pawn, bed) >= 2))
        {
            Job? job = TryMakePolyculeBedroomSleepJob(pawn, bed);
            if (job != null)
                return job;
        }
        return null;
    }

    private static int FreeSlots(Pawn pawn, Building_Bed bed)
    {
        return bed.SleepingSlotsCount - bed.GetComp<CompBedSharing>().GetBedmates().Count(p => p != pawn);
    }

    private static bool HasBedSharingPartner(Pawn pawn, Building_Bed bed)
    {
        return bed.GetComp<CompBedSharing>().GetBedmates().Any(bedmate => bedmate != pawn && PolyculeHelpers.AreBedSharingPartners(pawn, bedmate));
    }

    public static Job? TryMakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        foreach (int slot in GetAvailablePolyculeBedroomSlots(pawn, bed))
        {
            Job? job = BedSharingJobHelpers.TryMakeSleepJob(pawn, new SleepingPlace(bed, slot));
            if (job != null)
                return job;
        }
        return null;
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
            || bed.GetComp<CompBedSharing>() is not { } assignments)
            yield break;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        if (bed.IsOwner(pawn, out int? assignedSlot) && assignedSlot is int ownedSlot)
            slots = slots.Prepend(ownedSlot).Distinct();
        foreach (int slot in slots)
        {
            if (assignments.CanAssignSleepingSlot(pawn, slot))
                yield return slot;
        }
    }
}
