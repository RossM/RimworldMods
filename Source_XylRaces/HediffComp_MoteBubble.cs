namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_MoteBubble : HediffCompProperties
{
    public string? iconPath;

    public HediffCompProperties_MoteBubble()
    {
        compClass = typeof(HediffComp_MoteBubble);
    }
}

public class HediffComp_MoteBubble : HediffComp
{
    public HediffCompProperties_MoteBubble Props => (HediffCompProperties_MoteBubble)props;

    public MoteBubble? mote;

    public override void CompExposeData()
    {
        base.CompExposeData();

        Scribe_References.Look(ref mote, nameof(mote));
    }

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        mote = MoteMaker.MakeThoughtBubble(parent.pawn, Props.iconPath, maintain: true);
    }

    public override void CompPostPostRemoved()
    {
        if (mote?.Destroyed is false)
            mote.Destroy();
    }

    public override void CompPostTick(ref float severityAdjustment)
    {
        mote?.Maintain();
    }
}
