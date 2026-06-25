namespace XylXenos;

public class LoveEuphoriaProperties : GeneProperties
{
    public NeedDef need;
    public List<HediffDef> hediffs;
}

[UsedFromXml]
public class Gene_LoveEuphoria : GeneExt
{
    public LoveEuphoriaProperties Props => (LoveEuphoriaProperties)DefExt.props;

    public void Notify_PostLovin(Pawn partner)
    {
        if (!Props.hediffs.NullOrEmpty())
        {
            foreach (var hediffDef in Props.hediffs)
            {
                var hediff = partner.health.GetOrAddHediff(hediffDef);
                hediff.Severity = hediff.def.initialSeverity;
                if (hediff is HediffWithCompsExt hediffWithCompsExt)
                    hediffWithCompsExt.sourcePawn = pawn;
            }
        }

        partner.needs.TryGetNeed(Props.need)?.CurLevel = 1f;
    }

    public override void RegisterWith(NotificationManager manager)
    {
        base.RegisterWith(manager);
        manager.Register<Pawn>(NotificationDefOf.PostLovin, pawn, Notify_PostLovin);
    }
}
