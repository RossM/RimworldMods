namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_SeverityPerDay_Stage : HediffCompProperties_SeverityPerDay
{
    public IntRange showHoursToRecoverStages = IntRange.Invalid;
    public IntRange showDaysToRecoverStages = IntRange.Invalid;

    public HediffCompProperties_SeverityPerDay_Stage()
    {
        compClass = typeof(HediffComp_SeverityPerDay_Stage);
    }
}

/// <summary>
///     This is the same as <see cref="HediffComp_SeverityPerDay"/> except that the displayed time remaining is for only the current stage.
/// </summary>
[UsedFromXml]
public class HediffComp_SeverityPerDay_Stage : HediffComp_SeverityPerDay
{
    private HediffCompProperties_SeverityPerDay_Stage Props => (HediffCompProperties_SeverityPerDay_Stage)props;

    public override string? CompLabelInBracketsExtra
    {
        get
        {
            if ((Props.showHoursToRecover || Props.showHoursToRecoverStages.Includes(parent.CurStageIndex)) && SeverityChangePerDay() < 0f)
                return Mathf.RoundToInt((parent.Severity - parent.CurStage.minSeverity) / Mathf.Abs(SeverityChangePerDay()) * 24f).ToString() + "LetterHour".Translate();
            return null;
        }
    }

    public override string? CompTipStringExtra
    {
        get
        {
            if ((Props.showDaysToRecover || Props.showDaysToRecoverStages.Includes(parent.CurStageIndex)) && SeverityChangePerDay() < 0f)
                return "DaysToRecover".Translate(((parent.Severity - parent.CurStage.minSeverity) / Mathf.Abs(SeverityChangePerDay())).ToString("0.0")).Resolve();
            return null;
        }
    }
}
