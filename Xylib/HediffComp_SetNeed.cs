namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class HediffCompProperties_SetNeed : HediffCompProperties
{
    public required NeedDef need;
    public FloatRange range;
    public IntRange stages = new IntRange(int.MinValue, int.MaxValue);

    public HediffCompProperties_SetNeed()
    {
        compClass = typeof(HediffComp_SetNeed);
    }
}

[PublicAPI]
public class HediffComp_SetNeed : HediffComp
{
    public HediffCompProperties_SetNeed Props => (HediffCompProperties_SetNeed)props;

    private bool Active => Need != null && Props.stages.Includes(parent.CurStageIndex);

    public Need? Need
    {
        get
        {
            DebugAssert.NotNull(Pawn);

            return field ??= Pawn.needs.TryGetNeed(Props.need);
        }
    }

    public override string? CompTipStringExtra
    {
        get
        {
            if (!Active)
                return null;

            if (Props.range.min <= 0)
                return $"  - {Props.need.LabelCap}: {"max".Translate().CapitalizeFirst()} {Props.range.max.ToStringPercent()}";
            if (Props.range.max >= 1)
                return $"  - {Props.need.LabelCap}: {"min".Translate().CapitalizeFirst()} {Props.range.min.ToStringPercent()}";
            if (Props.range.min == Props.range.max)
                return $"  - {Props.need.LabelCap}: {Props.range.min.ToStringPercent()}";
            return
                $"  - {Props.need.LabelCap}: {Props.range.min.ToStringPercent()}-{Props.range.max.ToStringPercent()}";
        }
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (Active)
            Need?.CurLevelPercentage = Mathf.Clamp(Need.CurLevelPercentage, Props.range.min, Props.range.max);
    }
}
