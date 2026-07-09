namespace XylXenos;

[UsedFromXml]
public class Hediff_LovinAddiction : HediffWithCompsExt
{
    public Need_Lovin? Need
    {
        get
        {
            DebugAssert.NotNull(pawn);
            return field ??= pawn.needs.TryGetNeed<Need_Lovin>();
        }
    }

    public override int CurStageIndex => Need?.Satisfied is false ? 1 : 0;

    public override string? TipStringExtra
    {
        get
        {
            string? text = base.TipStringExtra;
            if (Need != null)
            {
                if (!text.NullOrEmpty())
                    text += "\n";
                text += $"{"CreatesNeed".Translate()}: {Need.LabelCap} ({Need.CurLevelPercentage.ToStringPercent("F0")})";
            }

            return text;
        }
    }

    public override string LabelInBrackets
    {
        get
        {
            string? labelInBrackets = base.LabelInBrackets;
            string text = (1f - Severity).ToStringPercent("F0");
            if (!labelInBrackets.NullOrEmpty())
                return labelInBrackets + ", " + text;
            return text;
        }
    }

    public void Notify_NeedCategoryChanged()
    {
        DebugAssert.NotNull(pawn);
        pawn.health.Notify_HediffChanged(this);
    }
}
