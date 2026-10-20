namespace Yobai;

internal static class BedSelectionHelpers
{
    public static Job? ChooseBedtimeSleepJob(Pawn pawn)
    {
        Job? sleep = ChooseOccupiedPolyculeBedroomSleepJob(pawn);
        if (sleep != null)
            return sleep;
        return ChooseEmptyPolyculeBedroomSleepJob(pawn) ?? ChooseFallbackPolyculeBedroomSleepJob(pawn);
    }

    private static Job? ChooseOccupiedPolyculeBedroomSleepJob(Pawn pawn)
    {
        return TryBedsInRandomOrder(GetPolyculeBedroomBeds(pawn)
            .Where(bed => bed.GetComp<CompBedSharing>()?.GetBedmates().Any(other => other != pawn) == true
                && HasOnlyPartnerBedmates(pawn, bed)),
            bed => TryMakePolyculeBedroomSleepJob(pawn, bed));
    }

    private static Job? ChooseEmptyPolyculeBedroomSleepJob(Pawn pawn)
    {
        return TryBedsInRandomOrder(GetPolyculeBedroomBeds(pawn)
            .Where(bed => bed.GetComp<CompBedSharing>()?.GetBedmates().Any(other => other != pawn) != true
                && HasOnlyPartnerBedmates(pawn, bed)),
            bed => TryMakePolyculeBedroomSleepJob(pawn, bed));
    }

    private static Job? ChooseFallbackPolyculeBedroomSleepJob(Pawn pawn)
    {
        return TryBedsInRandomOrder(GetPolyculeBedroomBeds(pawn).Where(bed => !HasOnlyPartnerBedmates(pawn, bed)),
            bed => TryMakePolyculeBedroomSleepJob(pawn, bed));
    }

    private static bool HasOnlyPartnerBedmates(Pawn pawn, Building_Bed bed)
    {
        // A booked arrival affects the choice even before that pawn reaches the bed.
        return bed.GetComp<CompBedSharing>()?.GetBedmates().All(other => other == pawn
            || PolyculeHelpers.AreBedSharingPartners(pawn, other)) == true;
    }

    public static Job? TryMakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        if (!BedSharingHelpers.CanChooseSharedBed(pawn)
            || !BedSharingHelpers.CanSleepInSharedBed(pawn, bed)
            || !BedSharingHelpers.IsBedInAssignedPolyculeBedroom(pawn, bed))
            return null;

        IEnumerable<int> slots = Enumerable.Range(0, bed.SleepingSlotsCount);
        if (bed.IsOwner(pawn, out int? assignedSlot) && assignedSlot is int ownedSlot)
            slots = slots.Prepend(ownedSlot).Distinct();
        return TryMakeSleepJobInSlots(pawn, bed, slots);
    }

    private static IEnumerable<Building_Bed> GetPolyculeBedroomBeds(Pawn pawn)
    {
        return pawn.ownership?.OwnedBed?.GetRoom()?.ContainedBeds ?? Enumerable.Empty<Building_Bed>();
    }

    private static Job? TryBedsInRandomOrder(IEnumerable<Building_Bed> beds, Func<Building_Bed, Job?> makeJob)
    {
        // Randomize beds, rather than slots, so beds with more free places have no advantage.
        foreach (Building_Bed bed in beds.InRandomOrder())
        {
            Job? job = makeJob(bed);
            if (job != null)
                return job;
        }
        return null;
    }

    private static Job? TryMakeSleepJobInSlots(Pawn pawn, Building_Bed bed, IEnumerable<int> candidateSlots)
    {
        CompBedSharing? assignments = bed.GetComp<CompBedSharing>();
        if (assignments == null)
            return null;

        foreach (int slot in candidateSlots)
        {
            if (!assignments.CanAssignSleepingSlot(pawn, slot))
                continue;
            Job? job = BedSharingJobHelpers.TryMakeSleepJob(pawn, new SleepingPlace(bed, slot));
            if (job != null)
                return job;
        }
        return null;
    }
}
