namespace Yobai;

internal static class BedSelectionHelpers
{
    public static Job? ChooseBedtimeSleepJob(Pawn pawn)
    {
        Job? sleep = ChooseOccupiedPolyculeBedroomSleepJob(pawn);
        if (sleep != null)
            return sleep;
        if (Rand.Chance(0.5f))
        {
            Job? visit = ChooseNightVisitSleepJob(pawn);
            if (visit != null)
                return visit;
        }
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

    public static Job? ChooseNightVisitSleepJob(Pawn pawn)
    {
        IEnumerable<Building_Bed> beds = PolyculeHelpers.GetBedSharingPartners(pawn)
            .Select(partner => partner.CurrentBed()).OfType<Building_Bed>().Distinct()
            .Where(bed => bed != pawn.CurrentBed());
        return TryBedsInRandomOrder(beds, bed => TryMakeNightVisitSleepJob(pawn, bed));
    }

    public static Job? TryMakePolyculeBedroomSleepJob(Pawn pawn, Building_Bed bed)
    {
        if (!BedSharingHelpers.CanChooseSharedBed(pawn)
            || !BedSharingHelpers.CanSleepInSharedBed(pawn, bed)
            || !BedSharingHelpers.IsBedInAssignedPolyculeBedroom(pawn, bed))
            return null;

        // A bedroom's temporary sleepers may use any of its beds' sleeping places.
        return TryMakeSleepJobInSlots(pawn, bed, Enumerable.Range(0, bed.SleepingSlotsCount));
    }

    public static Job? TryMakeNightVisitSleepJob(Pawn visitor, Building_Bed bed)
    {
        if (!BedSharingHelpers.CanChooseSharedBed(visitor)
            || !BedSharingHelpers.CanSleepInSharedBed(visitor, bed)
            || !BedSharingHelpers.IsBedEligibleForNightVisits(bed)
            || bed.GetComp<CompBedSharing>()?.NightVisitsEnabled != true)
            return null;

        List<Pawn> occupants = bed.CurOccupants.ToList();
        if (occupants.Count != 1)
            return null;
        Pawn host = occupants[0];
        if (!PolyculeHelpers.AreBedSharingPartners(visitor, host)
            || !HasOnlyPartnerBedmates(visitor, bed)
            || host.Awake() || !BedSharingHelpers.CanShareBedsNow(host))
            return null;

        // Permanent assignments use the first slots. A visit may use only the remaining slots.
        int firstUnassignedSlot = bed.OwnersForReading.Count;
        return TryMakeSleepJobInSlots(visitor, bed,
            Enumerable.Range(firstUnassignedSlot, bed.SleepingSlotsCount - firstUnassignedSlot));
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
            Job? job = SharedSleepJobHelpers.TryMakeSleepJob(pawn, new SleepingPlace(bed, slot));
            if (job != null)
                return job;
        }
        return null;
    }
}
