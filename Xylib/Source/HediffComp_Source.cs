namespace Xylib;

[UsedFromXml]
[PublicAPI]
public class HediffCompProperties_Source : HediffCompProperties
{
    public bool showName = true;

    public HediffCompProperties_Source()
    {
        compClass = typeof(HediffComp_Source);
    }
}

[PublicAPI]
public class HediffComp_Source : HediffComp
{
    public HediffCompProperties_Source Props => (HediffCompProperties_Source)props;

    public Pawn? OtherPawn => (Pawn?)other;
    public Thing? other;

    public override string? CompLabelInBracketsExtra
    {
        get
        {
            if (!Props.showName || other == null)
                return null;
            return other.LabelShort;
        }
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_References.Look(ref other, "other");
    }
}
