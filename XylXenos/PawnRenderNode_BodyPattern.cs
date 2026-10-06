namespace XylXenos;

[UsedFromXml]
public class PawnRenderNode_BodyPattern(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_Body(pawn, props, tree)
{
    public override Graphic GraphicFor(Pawn pawn)
    {
        string? maskPath = pawn.story?.bodyType?.bodyNakedGraphicPath;
        return GraphicDatabase.Get<Graphic_Multi>(TexPathFor(pawn), ShaderDatabase.CutoutSkinOverlay, Vector2.one, ColorFor(pawn),
            Color.white, null, maskPath);
    }
}
