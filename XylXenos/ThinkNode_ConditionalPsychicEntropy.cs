namespace XylXenos;

[UsedFromXml]
public class ThinkNode_ConditionalPsychicEntropy : ThinkNode_Conditional
{
    public float maximum;

    protected override bool Satisfied(Pawn pawn)
    {
        return pawn.psychicEntropy != null && pawn.psychicEntropy.EntropyValue <= maximum;
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        ThinkNode_ConditionalPsychicEntropy copy = (ThinkNode_ConditionalPsychicEntropy)base.DeepCopy(resolve);
        copy.maximum = maximum;
        return copy;
    }
}
