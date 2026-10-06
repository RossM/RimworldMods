using System.Diagnostics.CodeAnalysis;

namespace XylXenos;

[UsedFromXml]
public class ThinkNode_ConditionalHasGene : ThinkNode_Conditional
{
    public required GeneDef gene;

    protected override bool Satisfied(Pawn pawn)
    {
        return pawn.HasActiveGene(gene);
    }

    public override ThinkNode DeepCopy(bool resolve = true)
    {
        ThinkNode_ConditionalHasGene copy = (ThinkNode_ConditionalHasGene)base.DeepCopy(resolve);
        copy.gene = gene;
        return copy;
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override void ResolveReferences()
    {
        base.ResolveReferences();

        if (gene == null)
            Log.Warning($"{nameof(gene)} is null in {nameof(ThinkNode_ConditionalHasGene)}");
    }
}
