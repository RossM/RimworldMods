using System.Xml;

namespace XylXenos;

[UsedFromXml]
public class GeneCompProperties_EmergencyReserves : GeneCompProperties
{
    [UsedFromXml]
    public class HediffSeverity
    {
        public required HediffDef hediff;
        public float minSeverity;

        [UsedFromReflection]
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, nameof(hediff), xmlRoot.Name);
            if (xmlRoot.FirstChild?.Value is null)
                throw new InvalidOperationException();
            minSeverity = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }

    public required HediffDef hediff;
    public FloatRange severityRange = FloatRange.One;

    public required List<HediffSeverity> triggers;

    public GeneCompProperties_EmergencyReserves()
    {
        compClass = typeof(GeneComp_EmergencyReserves);
    }

    public override string? ExtraGeneEffectDescription()
    {
        if (hediff.stages?[^1] is null)
            return null;

        StringBuilder sb = new();
        sb.AppendLineTagged($"{"Active".Translate().CapitalizeFirst()}:".Colorize(ColoredText.TipSectionTitleColor));
        foreach (var stat in hediff.stages[^1].SpecialDisplayStats())
            sb.AppendLine($"- {stat.LabelCap}: {stat.ValueString}");
        return sb.ToString();
    }
}

public class GeneComp_EmergencyReserves : GeneComp
{
    public GeneCompProperties_EmergencyReserves Props => (GeneCompProperties_EmergencyReserves)props;

    // Ensure that the pawn doesn't get vacuum burns before the hediff triggers, but can still get them
    // during cooldown
    public override bool CompImmuneToVacuumBurns => !Pawn.health.hediffSet.HasHediff(Props.hediff);

    public override void CompTickInterval(int delta)
    {
        if (!Pawn.IsHashIntervalTick(60, delta))
            return;

        if (Pawn.health.hediffSet.HasHediff(Props.hediff))
            return;

        bool shouldTrigger = false;
        foreach (var hediffSeverity in Props.triggers)
        {
            if (Pawn.health.hediffSet.TryGetHediff(hediffSeverity.hediff, out var result) && result.Severity >= hediffSeverity.minSeverity)
            {
                shouldTrigger = true;
                break;
            }
        }

        if (!shouldTrigger)
            return;

        var hediff = Pawn.health.AddHediff(Props.hediff);
        hediff.Severity = Props.severityRange.RandomInRange;
    }
}
