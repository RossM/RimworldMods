namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_SetNeed : HediffCompProperties
{
    public NeedDef need;
    public float levelPercentage;
    public FloatRange severityRange = new(float.MinValue, float.MaxValue);

    public HediffCompProperties_SetNeed()
    {
        compClass = typeof(HediffComp_SetNeed);
    }
}

public class HediffComp_SetNeed : HediffComp
{
    public HediffCompProperties_SetNeed Props => (HediffCompProperties_SetNeed)props;

    public Need Need => field ??= Pawn.needs.TryGetNeed(Props.need);

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        if (Need != null && Props.severityRange.Includes(parent.Severity))
        {
            Need.CurLevelPercentage = Props.levelPercentage;
        }
    }
}
