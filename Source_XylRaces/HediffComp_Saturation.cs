using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_Saturation : HediffCompPropertiesExt
{
    public required HediffDef sourceHediff;
    public float severityLossPerDay;
    public float severityGainPerDay;

    public HediffDef? parent;

    public HediffCompProperties_Saturation()
    {
        compClass = typeof(HediffComp_Saturation);
    }

    // ReSharper disable once ParameterHidesMember
    public override void ResolveReferences(HediffDef parent)
    {
        this.parent = parent;
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest request)
    {
        if (severityGainPerDay > 0)
        {
            yield return new(StatCategoryDefOf.Drug, "XylSaturationGain".Translate(),
                "PerDay".Translate(severityGainPerDay.ToStringPercent()),
                "XylSaturationGainDesc".Translate(), 0);
            if (parent != null)
            {
                yield return new(StatCategoryDefOf.Drug, "XylDaysToFullSaturation".Translate(),
                    "PeriodDays".Translate((parent.maxSeverity / severityGainPerDay).ToStringDecimalIfSmall()),
                    "XylDaysToFullSaturationDesc".Translate(), 0);
            }
        }

        if (severityLossPerDay < 0)
        {
            yield return new(StatCategoryDefOf.Drug, "XylSaturationLoss".Translate(),
                "PerDay".Translate(severityLossPerDay.ToStringPercent()),
                "XylSaturationLossDesc".Translate(), 0);
        }
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        if (sourceHediff is null)
            yield return $"{nameof(sourceHediff)} is null";
    }
}

public class HediffComp_Saturation : HediffComp_SeverityModifierBase
{
    public HediffCompProperties_Saturation Props => (HediffCompProperties_Saturation)props;

    public override string CompLabelInBracketsExtra => (parent.Severity / parent.def.maxSeverity).ToStringPercent();

    public override float SeverityChangePerDay()
    {
        return Pawn!.health.hediffSet.HasHediff(Props.sourceHediff) ? Props.severityGainPerDay : Props.severityLossPerDay;
    }
}
