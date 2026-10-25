namespace XylXenos;

[UsedFromXml]
public class StatPart_Echolocation : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.Thing is Pawn pawn && PatchHelpers.IsUsingEcholocation(pawn))
            val = 1f;
    }

    public override string? ExplanationPart(StatRequest req) => null;
}
