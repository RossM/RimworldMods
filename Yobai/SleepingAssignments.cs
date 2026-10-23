namespace Yobai;

/// <summary>
/// Resolves and manages the sleeping place currently assigned to each pawn. Temporary assignments
/// are stored on bed comps, not in a separate map component.
/// </summary>
/// <remarks>
/// All assignment resolution and temporary-assignment rules are scoped to a single map.
/// A temporary assignment is active while its pawn lies in that slot or travels to it for sleep
/// or lovin'. It also survives its creation tick so job selection can finish before travel starts.
/// The pawn must be alive and spawned on the bed's map, and the bed must remain eligible for
/// sharing. Native bed reservations do not determine the assignment's lifetime.
///
/// A permanent owner's assignment is active only while the owner has no active temporary
/// assignment on this map and nobody has a temporary assignment in the owner's permanent slot.
/// Thus a temporary stay releases the pawn's home slot, including for night visits. Expiry restores
/// the permanent assignment unless another temporary sleeper still holds that slot.
/// On each map there is at most one temporary assignment per pawn and per slot; moving between beds replaces
/// the previous temporary assignment. Neither moving nor expiry changes permanent ownership.
/// Temporary use never grants permanent ownership, including to an otherwise unowned pawn.
/// An owner displaced by a temporary sleeper also keeps its permanent ownership. Otherwise,
/// automatic bed claims retain vanilla behavior, including replacing an existing active owned bed.
///
/// Physical occupancy is independent of assignment and always blocks another pawn's use of a slot.
/// Polycule bedtime selection may borrow a permanent place that its owner is not using, but cannot
/// displace an occupant or an incoming sleeper. Eligible bedroom slots are permanently owned by a
/// polycule member, or unowned in a bed with at least one permanent polycule owner. An unrelated
/// owner's empty slot is not eligible. Night visits require a currently unassigned place
/// (or the visitor's own place), rather than merely an empty place. Both features retain their
/// separate bedroom, relationship and bedmate-compatibility requirements.
/// </remarks>
internal static class SleepingAssignments
{
    public static IReadOnlyList<ActiveAssignment> GetActiveAssignments(Map? map)
    {
        if (map == null)
            return [];

        List<CompBedSharing> comps = GetBedComps(map).ToList();
        List<ActiveAssignment> temporaryAssignments = ReadTemporaryAssignments(comps).ToList();
        HashSet<Pawn> temporaryPawns = temporaryAssignments.Select(assignment => assignment.Pawn).ToHashSet();
        HashSet<(Building_Bed Bed, int Slot)> temporaryPlaces = temporaryAssignments
            .Select(assignment => (assignment.Place.Bed, assignment.Place.Slot)).ToHashSet();
        List<ActiveAssignment> active = temporaryAssignments.ToList();

        foreach (CompBedSharing comp in comps)
        {
            for (int slot = 0; slot < Math.Min(comp.Bed.OwnersForReading.Count, comp.Bed.SleepingSlotsCount); slot++)
            {
                Pawn owner = comp.Bed.OwnersForReading[slot];
                // Deathrest caskets have their own ownership, separate from the pawn's sleeping bed.
                if (owner.ownership?.OwnedBed != comp.Bed || temporaryPawns.Contains(owner) || temporaryPlaces.Contains((comp.Bed, slot)))
                    continue;
                active.Add(new ActiveAssignment(owner, new SleepingPlace(comp.Bed, slot), IsTemporary: false));
            }
        }
        return active;
    }

    public static SleepingPlace? GetTemporaryAssignment(Pawn pawn)
    {
        return GetActiveAssignments(pawn.Map).FirstOrDefault(assignment => assignment.IsTemporary && assignment.Pawn == pawn)?.Place;
    }

    public static SleepingPlace? GetActiveAssignment(Pawn pawn)
    {
        return GetActiveAssignments(pawn.Map).FirstOrDefault(assignment => assignment.Pawn == pawn)?.Place;
    }

    public static Pawn? GetAssignedPawn(SleepingPlace place)
    {
        return GetActiveAssignments(place.Bed.Map).FirstOrDefault(assignment => assignment.Place.Bed == place.Bed && assignment.Place.Slot == place.Slot)?.Pawn;
    }

    public static IEnumerable<Pawn> GetBedmates(Building_Bed bed)
    {
        IEnumerable<Pawn> sleepers = GetActiveAssignments(bed.Map)
            .Where(assignment => assignment.Place.Bed == bed && assignment.IsInUse).Select(assignment => assignment.Pawn);
        return bed.CurOccupants.Concat(sleepers).Distinct();
    }

    private static IEnumerable<ActiveAssignment> ReadTemporaryAssignments(IEnumerable<CompBedSharing> comps)
    {
        foreach (CompBedSharing comp in comps)
        {
            foreach (TemporarySleepingSlot assignment in comp.TemporaryAssignments)
            {
                if (assignment.Pawn is { } pawn)
                    yield return new ActiveAssignment(pawn, new SleepingPlace(comp.Bed, assignment.Slot), IsTemporary: true);
            }
        }
    }

    public static bool IsAvailable(Pawn pawn, SleepingPlace place)
    {
        Pawn? occupant = place.Bed.GetCurOccupant(place.Slot);
        if (occupant != null && occupant != pawn)
            return false;
        ActiveAssignment? assignment = GetActiveAssignments(place.Bed.Map)
            .FirstOrDefault(assignment => assignment.Place.Bed == place.Bed && assignment.Place.Slot == place.Slot);
        return assignment == null || assignment.Pawn == pawn || !assignment.IsInUse;
    }

    public static bool CanAssignForNightVisit(Pawn pawn, SleepingPlace place)
    {
        Pawn? assignee = GetAssignedPawn(place);
        return (assignee == null || assignee == pawn) && CanAssignTemporary(pawn, place);
    }

    public static bool CanAssignTemporary(Pawn pawn, SleepingPlace place)
    {
        if (place.Bed.GetComp<CompBedSharing>() == null || !IsAvailable(pawn, place))
            return false;

        // Selecting another place in the same bed must not overwrite an existing stay.
        // Transfers to another bed let a sleeping pawn leave for a night visit.
        if (GetTemporaryAssignment(pawn)?.Bed == place.Bed)
            return false;
        return BedSharingHelpers.CanSleepInSharedBed(pawn, place.Bed);
    }

    public static bool TryAssignTemporary(Pawn pawn, SleepingPlace place)
    {
        if (!CanAssignTemporary(pawn, place))
            return false;

        SleepingPlace? previous = GetTemporaryAssignment(pawn);
        CompBedSharing? previousComp = previous?.Bed.GetComp<CompBedSharing>();
        TemporarySleepingSlot? previousAssignment = previousComp?.TemporaryAssignments.FirstOrDefault(assignment => assignment.Pawn == pawn);
        if (previousAssignment != null)
            previousComp!.RemoveAssignment(previousAssignment);

        CompBedSharing comp = place.Bed.GetComp<CompBedSharing>();
        TemporarySleepingSlot assignment = new(pawn, place.Slot);
        comp.AddAssignment(assignment);
        if (RestUtility.IsValidBedFor(place.Bed, pawn, pawn, true, guestStatus: pawn.GuestStatus))
            return true;

        comp.RemoveAssignment(assignment);
        if (previousAssignment != null)
            previousComp!.AddAssignment(previousAssignment);
        return false;
    }

    internal static bool IsActive(TemporarySleepingSlot assignment, Building_Bed bed)
    {
        if (!BedSharingHelpers.IsBedEligibleForSharing(bed)
            || assignment.Pawn is not { } pawn || !IsOnBedMap(pawn, bed)
            || assignment.Slot < 0 || assignment.Slot >= bed.SleepingSlotsCount)
            return false;

        return assignment.TickCreated == Find.TickManager.TicksGame || IsUsingPlace(pawn, new SleepingPlace(bed, assignment.Slot));
    }

    private static bool IsUsingPlace(Pawn pawn, SleepingPlace place)
    {
        if (!IsOnBedMap(pawn, place.Bed))
            return false;
        IntVec3 position = place.Bed.GetSleepingSlotPos(place.Slot);
        if (pawn.GetPosture().InBed() && pawn.Position == position)
            return true;

        Job? job = pawn.CurJob;
        bool goingToBed = pawn.jobs.curDriver is JobDriver_LayDown && job?.targetA.Thing == place.Bed
                          || pawn.jobs.curDriver is JobDriver_Lovin && job?.targetB.Thing == place.Bed;
        if (!goingToBed)
            return false;

        // Pre-toil validation runs after the job is accepted but before GotoBed starts its path.
        return pawn.jobs.curDriver.CurToilIndex < 0 || pawn.Position == position
               || pawn.pather.Moving && pawn.pather.Destination.Cell == position;
    }

    private static bool IsOnBedMap(Pawn pawn, Building_Bed bed)
    {
        return pawn.Spawned && !pawn.Dead && pawn.Map == bed.Map;
    }

    private static IEnumerable<CompBedSharing> GetBedComps(Map map)
    {
        foreach (Building_Bed bed in map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>())
        {
            if (bed.GetComp<CompBedSharing>() is { } comp)
                yield return comp;
        }
    }

    internal sealed record ActiveAssignment(Pawn Pawn, SleepingPlace Place, bool IsTemporary)
    {
        // Temporary records in this view have already passed the lifetime check, including the
        // selection grace period. An absent permanent owner keeps an assignment but permits borrowing.
        public bool IsInUse => IsTemporary || IsUsingPlace(Pawn, Place);
    }
}
