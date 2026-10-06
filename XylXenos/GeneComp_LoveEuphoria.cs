namespace XylXenos;

public class GeneCompProperties_LoveEuphoria : GeneCompProperties
{
    public required NeedDef need;
    public HediffDef? highHediff;
    public HediffDef? addictionHediff;
    public float addictionChance = 1f;

    public float needOffset = 1f;
    public List<HediffDef>? hediffs;
    public float maxLovinMtbHours = -1f;

    public GeneCompProperties_LoveEuphoria()
    {
        compClass = typeof(GeneComp_LoveEuphoria);
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        foreach (var stat in base.SpecialDisplayStats(req))
            yield return stat;

        if (highHediff?.CompProps<HediffCompProperties_SeverityPerDay>() is { } highSeverityPerDay)
        {
            var highDuration = highHediff.initialSeverity / -highSeverityPerDay.severityPerDay;
            yield return new StatDrawEntry(StatCategoryDefOf.Drug, "HighDuration".Translate(),
                "PeriodDays".Translate(highDuration.ToString("F1")), "Stat_Thing_Drug_HighDurationPerDose_Desc".Translate(), 2460);
        }

        if (addictionHediff is not null)
        {
            yield return new StatDrawEntry(StatCategoryDefOf.DrugAddiction, "Addictiveness".Translate(), addictionChance.ToStringPercent(), "Stat_Thing_Drug_Addictiveness_Desc".Translate(), 2428);
            yield return new StatDrawEntry(StatCategoryDefOf.DrugAddiction, "AddictionNeedOffset".Translate(), needOffset.ToStringPercent(), "Stat_Thing_Drug_AddictionNeedOffset_Desc".Translate(), 2420);
            yield return new StatDrawEntry(StatCategoryDefOf.DrugAddiction, "AddictionNeedFallRate".Translate(), "PerDay".Translate(need.fallPerDay.ToStringPercent()), "Stat_Thing_Drug_AddictionNeedFallRate_Desc".Translate(), 2410);
            yield return new StatDrawEntry(StatCategoryDefOf.DrugAddiction, "AddictionNeedDoseInterval".Translate(), "PeriodDays".Translate((needOffset / need.fallPerDay).ToString("F1")), "Stat_Thing_Drug_AddictionNeedDoseInterval_Desc".Translate(), 2400);
        }

        if (addictionHediff?.CompProps<HediffCompProperties_SeverityPerDay>() is { } addictionSeverityPerDay)
        {
            var addictionDuration = addictionHediff.initialSeverity / -addictionSeverityPerDay.severityPerDay;
            yield return new StatDrawEntry(StatCategoryDefOf.DrugAddiction, "AddictionRecoveryTime".Translate(),
                "PeriodDays".Translate(addictionDuration.ToString("F1")), "Stat_Thing_Drug_AddictionRecoveryTime_Desc".Translate(), 2395);
        }
    }
}

[UsedFromXml]
public class GeneComp_LoveEuphoria : GeneComp, IEventListener
{
    public GeneCompProperties_LoveEuphoria Props => (GeneCompProperties_LoveEuphoria)props;

    public void Notify_PostLovin(Pawn partner)
    {
        if (!Active)
            return;

        if (Rand.Chance(Props.addictionChance))
            AddHediff(partner, Props.addictionHediff);

        AddHediff(partner, Props.highHediff);

        partner.needs.TryGetNeed(Props.need)?.CurLevel += Props.needOffset;
    }

    private void AddHediff(Pawn partner, HediffDef? hediffDef)
    {
        if (hediffDef is null)
            return;

        var hediff = partner.health.GetOrAddHediff(hediffDef);
        hediff.Severity = hediff.def.initialSeverity;
        if (hediff.TryGetComp<HediffComp_Source>() is { } source)
            source.other = Pawn;
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register<Pawn>(EventDefOf.PostLovin, Pawn, Notify_PostLovin);
    }

    public void PreUnregister(EventManager manager) { }
}
