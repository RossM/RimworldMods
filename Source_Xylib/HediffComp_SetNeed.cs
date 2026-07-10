namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class HediffCompProperties_SetNeed : HediffCompProperties
{
    public required NeedDef need;
    public FloatRange levelPercentage;
    public FloatRange severityRange = new(float.MinValue, float.MaxValue);

    public HediffCompProperties_SetNeed()
    {
        compClass = typeof(HediffComp_SetNeed);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        if (need is null)
            yield return $"{nameof(need)} is null";
    }
}

[PublicAPI]
public class HediffComp_SetNeed : HediffComp
{
    public HediffCompProperties_SetNeed Props => (HediffCompProperties_SetNeed)props;

    public Need? Need
    {
        get
        {
            DebugAssert.NotNull(Pawn);

            return field ??= Pawn.needs.TryGetNeed(Props.need);
        }
    }

    private bool Active => Need != null && Props.severityRange.Includes(parent.Severity);

    public override string? CompTipStringExtra
    {
        get
        {
            if (!Active)
                return null;

            if (Props.levelPercentage.min <= 0)
                return $"  - {Props.need.LabelCap}: {"max".Translate().CapitalizeFirst()} {Props.levelPercentage.max.ToStringPercent()}";
            if (Props.levelPercentage.max >= 1)
                return $"  - {Props.need.LabelCap}: {"min".Translate().CapitalizeFirst()} {Props.levelPercentage.min.ToStringPercent()}";
            if (Props.levelPercentage.min == Props.levelPercentage.max)
                return $"  - {Props.need.LabelCap}: {Props.levelPercentage.min.ToStringPercent()}";
            return
                $"  - {Props.need.LabelCap}: {Props.levelPercentage.min.ToStringPercent()}-{Props.levelPercentage.max.ToStringPercent()}";
        }
    }

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (Active)
            Need?.CurLevelPercentage = Mathf.Clamp(Need.CurLevelPercentage, Props.levelPercentage.min, Props.levelPercentage.max);
    }
}
