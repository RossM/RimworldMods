namespace Yobai;

/// <summary>
///     Resolves and manages the sleeping place currently assigned to each pawn. Temporary assignments
///     are stored on bed comps, not in a separate map component.
/// </summary>
/// <remarks>
///     All assignment resolution and temporary-assignment rules are scoped to a single map.
///     A temporary assignment is active while its pawn lies in that slot or travels to it for sleep
///     or lovin'. It also survives its creation tick so job selection can finish before travel starts.
///     The pawn must be alive and spawned on the bed's map, and the bed must remain eligible for
///     sharing. Native bed reservations do not determine the assignment's lifetime.
///     A permanent owner's assignment is active only while the owner has no active temporary
///     assignment on this map and nobody has a temporary assignment in the owner's permanent slot.
///     Thus a temporary stay releases the pawn's home slot, including for night visits. Expiry restores
///     the permanent assignment unless another temporary sleeper still holds that slot.
///     On each map there is at most one temporary assignment per pawn and per slot; moving between beds replaces
///     the previous temporary assignment. Neither moving nor expiry changes permanent ownership.
///     Temporary use never grants permanent ownership, including to an otherwise unowned pawn.
///     An owner displaced by a temporary sleeper also keeps its permanent ownership. Otherwise,
///     automatic bed claims retain vanilla behavior, including replacing an existing active owned bed.
///     Physical occupancy is independent of assignment and always blocks another pawn's use of a slot.
///     Polycule bedtime selection may borrow a permanent place that its owner is not using, but cannot
///     displace an occupant or an incoming sleeper. Eligible bedroom slots are permanently owned by a
///     polycule member, or unowned in a bed with at least one permanent polycule owner. An unrelated
///     owner's empty slot is not eligible. Night visits require a currently unassigned place
///     (or the visitor's own place), rather than merely an empty place. Both features retain their
///     separate bedroom, relationship and bedmate-compatibility requirements.
/// </remarks>
[UsedImplicitly]
internal class SleepingAssignments(Map map) : MapComponent(map)
{
    internal sealed record ActiveAssignment(Pawn Pawn, SleepingPlace Place, bool IsTemporary)
    {
        // Temporary records in this view have already passed the lifetime check, including the
        // selection grace period. An absent permanent owner keeps an assignment but permits borrowing.
        public bool IsInUse => IsTemporary || Place.BeingUsedBy(Pawn);
    }

    public IEnumerable<ActiveAssignment> GetActiveAssignments()
    {
        HashSet<Pawn> seenPawns = [];
        HashSet<SleepingPlace> seenPlaces = [];

        List<CompBedSharing> comps = [.. GetBedComps()];
        foreach (var comp in comps)
        {
            foreach ((Pawn pawn, int slot) in comp.TemporaryAssignments)
            {
                SleepingPlace sleepingPlace = comp.Bed.GetSleepingPlace(slot);
                if (seenPawns.Add(pawn) && seenPlaces.Add(sleepingPlace))
                    yield return new ActiveAssignment(pawn, sleepingPlace, IsTemporary: true);
            }
        }

        foreach (var comp in comps)
        {
            for (int slot = 0; slot < Math.Min(comp.Bed.OwnersForReading.Count, comp.Bed.SleepingSlotsCount); slot++)
            {
                Pawn pawn = comp.Bed.OwnersForReading[slot];

                // Deathrest caskets have their own ownership, separate from the pawn's sleeping bed.
                if (pawn.ownership.OwnedBed != comp.Bed)
                    continue;

                SleepingPlace sleepingPlace = comp.Bed.GetSleepingPlace(slot);
                if (seenPawns.Add(pawn) && seenPlaces.Add(sleepingPlace))
                    yield return new ActiveAssignment(pawn, sleepingPlace, IsTemporary: false);
            }
        }
    }

    public Pawn? GetAssignedPawn(SleepingPlace place)
    {
        return GetActiveAssignments()
            .FirstOrDefault(assignment => assignment.Place.Bed == place.Bed && assignment.Place.Slot == place.Slot)?.Pawn;
    }

    private IEnumerable<CompBedSharing> GetBedComps()
    {
        foreach (Building_Bed bed in map.listerBuildings.allBuildingsColonist.OfType<Building_Bed>())
        {
            if (bed.GetComp<CompBedSharing>() is { } comp)
                yield return comp;
        }
    }
}
