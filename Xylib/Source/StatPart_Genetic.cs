namespace Xylib;

[UsedFromXml]
public class StatPart_Genetic : StatPart
{
    public required GeneDef gene;
    public float factor = 1f;

    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req is { HasThing: true, Thing: Pawn pawn } && pawn.HasActiveGene(gene))
            val *= factor;
    }

    public override string? ExplanationPart(StatRequest req)
    {
        if (req is { HasThing: true, Thing: Pawn pawn } && pawn.HasActiveGene(gene))
            return $"{gene.LabelCap}: x{factor}";
        return null;
    }
}
