using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class DefModExtension_WorkGiver_InteractWithPawn : DefModExtension
{
    public required JobDef job;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (job is null)
            yield return $"{nameof(job)} is null";
    }
}

[UsedFromXml]
public class WorkGiver_InteractWithPawn : WorkGiver_Scanner
{
    public DefModExtension_WorkGiver_InteractWithPawn DefExt => def.GetModExtension<DefModExtension_WorkGiver_InteractWithPawn>()!;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        DebugAssert.NotNull(pawn.Map);

        return pawn.Map.mapPawns.AllPawns.Where(targetPawn =>
            (targetPawn is { guest.IsPrisoner: true } ? targetPawn.guest.HostFaction : targetPawn.Faction) == pawn.Faction).ToList();
    }

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return PotentialWorkThingsGlobal(pawn).All(thing => !HasJobOnThing(pawn, thing, forced));
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn target)
            return false;
        if (pawn == target)
            return false;

        DebugAssert.NotNull(DefExt.job.driverClass);

        var worker = GenWorker<JobDriver_InteractWithPawn>.Get(DefExt.job.driverClass);
        worker.pawn = pawn;
        if (!worker.ValidateTarget(target))
            return false;

        if (!target.CanCasuallyInteractNow())
            return false;
        if (!pawn.CanReserve(target))
            return false;

        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return JobMaker.MakeJob(DefExt.job, t);
    }
}
