using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class HediffCompProperties_MoteBubble : HediffCompProperties
{
    public required string iconPath;

    public HediffCompProperties_MoteBubble()
    {
        compClass = typeof(HediffComp_MoteBubble);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
    {
        if (iconPath is null)
            yield return $"{nameof(iconPath)} is null";
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
        DebugAssert.NotNull(parent.pawn);

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
