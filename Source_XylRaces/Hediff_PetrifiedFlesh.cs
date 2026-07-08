namespace XylXenos;

[UsedFromXml]
public class Hediff_PetrifiedFlesh : HediffWithCompsExt
{
    public virtual float RelativeSeverity => Severity / Part.def.GetMaxHealth(pawn);

    public override int CurStageIndex => def.StageAtSeverity(RelativeSeverity);

    public override Color LabelColor => RelativeSeverity >= 1.0f ? FullyPetrifiedColor : base.LabelColor;

    public override string? SeverityLabel => Severity == 0f ? null : Severity.ToString("F1");

    private static readonly Color FullyPetrifiedColor = new(0.5f, 0.5f, 0.5f);

    public override float Severity
    {
        get => base.Severity;
        set => base.Severity = Mathf.Min(value, Part.def.GetMaxHealth(pawn));
    }

    protected override void UpdateCurStage(HediffStage stage)
    {
        base.UpdateCurStage(stage);

        stage.partEfficiencyOffset *= RelativeSeverity;
    }
}
