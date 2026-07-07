namespace XylXenos;

public class GeneCompProperties_LoveEuphoria : GeneCompProperties
{
    public NeedDef need;
    public float needOffset = 1f;
    public List<HediffDef> hediffs;

    public GeneCompProperties_LoveEuphoria()
    {
        compClass = typeof(GeneComp_LoveEuphoria);
    }
}

[UsedFromXml]
public class GeneComp_LoveEuphoria : GeneComp, IEventListener
{
    public GeneCompProperties_LoveEuphoria Props => (GeneCompProperties_LoveEuphoria)props;

    public void Notify_PostLovin(Pawn partner)
    {
        if (!Props.hediffs.NullOrEmpty())
        {
            foreach (var hediffDef in Props.hediffs)
            {
                var hediff = partner.health.GetOrAddHediff(hediffDef);
                hediff.Severity = hediff.def.initialSeverity;
                if (hediff.TryGetComp<HediffComp_Source>() is { } source)
                    source.other = Pawn;
            }
        }

        partner.needs.TryGetNeed(Props.need)?.CurLevel += Props.needOffset;
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register<Pawn>(EventDefOf.PostLovin, Pawn, Notify_PostLovin);
    }

    public void PreUnregister(EventManager manager)
    {
    }
}
