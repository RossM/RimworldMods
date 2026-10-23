namespace Yobai.Patches;

[Patch(typeof(Toils_Bed))]
internal static class Patch_Toils_Bed
{
    [Postfix]
    [Target(nameof(Toils_Bed.GotoBed))]
    public static void GotoBed_Postfix(TargetIndex bedIndex, Toil __result)
    {
        Action gotoBed = __result.initAction;
        __result.initAction = () =>
        {
            Pawn visitor = __result.actor;
            if (visitor.CurJobDef == JobDefOf.Lovin
                && visitor.CurJob.GetTarget(bedIndex).Thing is Building_Bed bed
                && bed.GetComp<CompBedSharing>()?.TryGetSleepingSlot(visitor, out _) == true
                && visitor.Position != RestUtility.GetBedSleepingSlotPosFor(visitor, bed))
            {
                Pawn partner = (Pawn)visitor.CurJob.targetA.Thing;

                bool PartnerUnavailable() => partner.CurrentBed() != bed
                                             || !NightVisitHelpers.AreReadyForLovin(visitor, partner);

                // A travelling visitor must not summon a departed partner back to bed.
                // Ordinary lovin' already starts at the sleeping place and keeps vanilla's checks.
                if (PartnerUnavailable())
                {
                    visitor.jobs.EndCurrentJob(JobCondition.Incompletable);
                    return;
                }

                __result.FailOn(PartnerUnavailable);
            }

            gotoBed();
        };
    }

    [Postfix]
    [Target(nameof(Toils_Bed.ClaimBedIfNonMedical))]
    public static void ClaimBedIfNonMedical_Postfix(TargetIndex ind, TargetIndex claimantIndex, Toil __result)
    {
        Action claimBed = __result.initAction;
        __result.initAction = () =>
        {
            Pawn actor = __result.actor;
            Pawn claimant = claimantIndex == TargetIndex.None
                ? actor
                : (Pawn)actor.CurJob.GetTarget(claimantIndex).Thing;
            SleepingAssignments.ActiveAssignment? assignment = SleepingAssignments.GetActiveAssignments(claimant.Map)
                .FirstOrDefault(assignment => assignment.Pawn == claimant);
            if (assignment is { IsTemporary: true })
                return;
            if (claimant.ownership?.OwnedBed is { } ownedBed && ownedBed.Map == claimant.Map && assignment == null)
                return;

            // Outside temporary stays or displacement, vanilla may claim an unowned sleeping place
            // even when the pawn already owns a bed. This lets pawns claim a bed on another map.
            claimBed();
        };
    }
}
