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
            Building_Bed bed = (Building_Bed)actor.CurJob.GetTarget(ind).Thing;
            // Automatic claims must preserve temporary stays. Manual assignments still work normally.
            if (bed.GetComp<CompBedSharing>()?.TryGetSleepingSlot(claimant, out _) != true)
                claimBed();
        };
    }
}
