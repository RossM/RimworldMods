namespace XylXenos;

public class HediffWithCompsExt : HediffWithComps
{
    private static readonly StringBuilder tipSb = new();
    protected HediffStage curStageInternal;
    public Pawn sourcePawn;

    public override HediffStage CurStage
    {
        get
        {
            if (curStageInternal == null)
            {
                curStageInternal = def.stages[CurStageIndex].MemberwiseClone();
                UpdateCurStage(curStageInternal);
            }

            return curStageInternal;
        }
    }

    public override float Severity
    {
        get => base.Severity;
        set
        {
            base.Severity = value;
            curStageInternal = null;
        }
    }

    public void Notify_CompChanged()
    {
        curStageInternal = null;
    }

    protected virtual void UpdateCurStage(HediffStage stage)
    {
        foreach (var comp in comps)
        {
            if (comp is HediffComp_GrowthModeExt { CausesNoPain: true })
                stage.painOffset = 0f;
        }
    }

    public override bool TendableNow(bool ignoreTimer = false)
    {
        if (!base.TendableNow(ignoreTimer))
            return false;

        foreach (var comp in comps)
        {
            if (comp is HediffComp_GrowthModeExt { AllowTend: false })
                return false;
        }

        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref sourcePawn, nameof(sourcePawn));
    }

    // ReSharper disable once ParameterHidesMember
    public override string GetTooltip(Pawn pawn, bool showHediffsDebugInfo)
    {
        tipSb.Clear();
        HediffStage curStage = CurStage;
        if (!LabelCap.NullOrEmpty())
            tipSb.AppendTagged(LabelCap.Colorize(ColoredText.TipSectionTitleColor));
        string severityLabel = SeverityLabel;
        if (!severityLabel.NullOrEmpty())
            tipSb.Append(": ").Append(severityLabel);
        tipSb.AppendLine();
        if (!def.overrideTooltip.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(def.overrideTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("PARTNER")));
        else if (curStage != null && !curStage.overrideTooltip.NullOrEmpty())
        {
            tipSb.AppendLine().AppendLineTagged(curStage.overrideTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("PARTNER")));
        }
        else
        {
            string description = Description;
            if (!description.NullOrEmpty())
                tipSb.AppendLine().AppendLine(description);
        }

        if (!def.extraTooltip.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(def.extraTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("PARTNER")));
        if (curStage != null && !curStage.extraTooltip.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(curStage.extraTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("PARTNER")));
        string tipStringExtra = TipStringExtra;
        if (!tipStringExtra.NullOrEmpty())
            tipSb.AppendLine().AppendLine(tipStringExtra.TrimEndNewlines());
        if (HealthCardUtility.GetCombatLogInfo(Gen.YieldSingle(this), out var taggedString, out var _) && !taggedString.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(("Cause".Translate() + ": " + taggedString).Colorize(ColoredText.SubtleGrayColor));
        if (showHediffsDebugInfo && !DebugString().NullOrEmpty() && !DebugString().NullOrEmpty())
            tipSb.AppendLine().AppendLine(DebugString().TrimEndNewlines());
        return tipSb.ToString().TrimEnd();
    }
}
