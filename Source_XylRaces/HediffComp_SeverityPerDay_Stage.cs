namespace XylXenos;

/// <summary>
///     This is the same as <see cref="HediffComp_SeverityPerDay"/> except that the displayed time remaining is for only the current stage.
/// </summary>
[UsedFromXml]
public class HediffComp_SeverityPerDay_Stage : HediffComp_SeverityPerDay
{
    private HediffCompProperties_SeverityPerDay Props => (HediffCompProperties_SeverityPerDay)props;

    public override string? CompLabelInBracketsExtra
    {
        get
        {
            if (Props.showHoursToRecover && SeverityChangePerDay() < 0f)
                return Mathf.RoundToInt((parent.Severity - parent.CurStage.minSeverity) / Mathf.Abs(SeverityChangePerDay()) * 24f).ToString() + "LetterHour".Translate();
            return null;
        }
    }

    public override string? CompTipStringExtra
    {
        get
        {
            if (Props.showDaysToRecover && SeverityChangePerDay() < 0f)
                return "DaysToRecover".Translate(((parent.Severity - parent.CurStage.minSeverity) / Mathf.Abs(SeverityChangePerDay())).ToString("0.0")).Resolve();
            return null;
        }
    }
}
