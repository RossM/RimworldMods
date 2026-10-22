namespace Yobai;

internal static class BedSelectionHelpers
{
    private enum BedPreference
    {
        DirectPartners,
        Empty,
        OtherPolyculeMembers,
    }

    public static Job? ChooseBedtimeSleepJob(Pawn pawn)
    {
        List<Building_Bed> beds = GetPolyculeBedroomBeds(pawn)
            .Where(bed => GetAvailablePolyculeBedroomSlots(pawn, bed).Any()).ToList();
        beds.Shuffle();

        // OrderBy preserves the shuffled order among beds with the same preference.
        foreach (Building_Bed bed in beds.OrderBy(bed => GetBedPreference(pawn, bed)))
        {
            Job? job = TryMakePolyculeBedroomSleepJob(pawn, bed);
            if (job != null)
                return job;
        }
        return null;
    }

    private static BedPreference GetBedPreference(Pawn pawn, Building_Bed bed)
    {
        bool hasPartner = false;
        foreach (Pawn bedmate in bed.GetComp<CompBedSharing>().GetBedmates())
        {
            if (bedmate == pawn)
                continue;
            if (!PolyculeHelpers.AreBedSharingPartners(pawn, bedmate))
                return BedPreference.OtherPolyculeMembers;
            hasPartner = true;
        }
        return hasPartner ? BedPreference.DirectPartners : BedPreference.Empty;
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
