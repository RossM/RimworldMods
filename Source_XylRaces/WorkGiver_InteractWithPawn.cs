namespace XylXenos;

public class WorkGiverDefExtension_InteractWithPawn : DefModExtension
{
    public JobDef job;
}

[UsedFromXml]
public class WorkGiver_InteractWithPawn : WorkGiver_Scanner
{
    public WorkGiverDefExtension_InteractWithPawn DefExt => def.GetModExtension<WorkGiverDefExtension_InteractWithPawn>();

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.mapPawns.AllPawns.Where(targetPawn =>
            (targetPawn.IsPrisoner ? targetPawn.guest.HostFaction : targetPawn.Faction) == pawn.Faction).ToList();
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