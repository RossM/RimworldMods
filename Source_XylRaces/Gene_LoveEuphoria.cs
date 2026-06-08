namespace XylXenos;

public class LoveEuphoriaInfo
{
    public NeedDef need;
    public List<HediffDef> hediffs;
}

[UsedFromXml]
public class Gene_LoveEuphoria : GeneExt
{
    public LoveEuphoriaInfo Info => DefExt.loveEuphoria;

    public void Notify_PostLovin(Pawn partner)
    {
        if (!Info.hediffs.NullOrEmpty())
        {
            foreach (var hediffDef in Info.hediffs)
            {
                var hediff = partner.health.GetOrAddHediff(hediffDef);
                hediff.Severity = hediff.def.initialSeverity;
                if (hediff is HediffWithCompsExt hediffWithCompsExt)
                    hediffWithCompsExt.sourcePawn = pawn;
            }
        }

        partner.needs.TryGetNeed(Info.need)?.CurLevel = 1f;
    }

    public override void RegisterWith(NotificationManager manager)
    {
        base.RegisterWith(manager);
        manager.Register<Pawn>(NotificationDefOf.PostLovin, pawn, Notify_PostLovin);
    }
}
