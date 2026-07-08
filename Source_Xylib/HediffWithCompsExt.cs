namespace Xylib;

[PublicAPI]
public class HediffWithCompsExt : HediffWithComps
{
    public Pawn SourcePawn => GetComp<HediffComp_Source>()?.OtherPawn;

    private static readonly StringBuilder tipSb = new();
    protected HediffStage curStageInternal;

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

    protected virtual void UpdateCurStage(HediffStage stage)
    {
        foreach (var comp in comps)
        {
            if (comp is IHediffCompExt { CausesNoPain: true })
                stage.painOffset = 0f;
        }
    }

    public override bool TendableNow(bool ignoreTimer = false)
    {
        if (!base.TendableNow(ignoreTimer))
            return false;

        foreach (var comp in comps)
        {
            if (comp is IHediffCompExt { AllowTend: false })
                return false;
        }

        return true;
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

        Pawn sourcePawn = SourcePawn;

        if (!def.overrideTooltip.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(def.overrideTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("SOURCE")));
        else if (curStage != null && !curStage.overrideTooltip.NullOrEmpty())
        {
            tipSb.AppendLine().AppendLineTagged(curStage.overrideTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("SOURCE")));
        }
        else
        {
            string description = Description;
            if (!description.NullOrEmpty())
                tipSb.AppendLine().AppendLine(description);
        }

        if (!def.extraTooltip.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(def.extraTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("SOURCE")));
        if (curStage != null && !curStage.extraTooltip.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(curStage.extraTooltip.Formatted(pawn.Named("PAWN"), sourcePawn.Named("SOURCE")));
        string tipStringExtra = TipStringExtra;
        if (!tipStringExtra.NullOrEmpty())
            tipSb.AppendLine().AppendLine(tipStringExtra.TrimEndNewlines());
        if (HealthCardUtility.GetCombatLogInfo(Gen.YieldSingle(this), out var taggedString, out _) && !taggedString.NullOrEmpty())
            tipSb.AppendLine().AppendLineTagged(("Cause".Translate() + ": " + taggedString).Colorize(ColoredText.SubtleGrayColor));
        if (showHediffsDebugInfo && !DebugString().NullOrEmpty() && !DebugString().NullOrEmpty())
            tipSb.AppendLine().AppendLine(DebugString().TrimEndNewlines());
        return tipSb.ToString().TrimEnd();
    }

    public void Notify_CompStateChange()
    {
        curStageInternal = null;
    }
}
