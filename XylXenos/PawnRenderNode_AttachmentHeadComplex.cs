namespace XylXenos;

[UsedFromXml]
public class PawnRenderNode_AttachmentHeadComplex(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode_AttachmentHead(pawn, props, tree)
{
    protected override Shader DefaultShader => ShaderDatabase.CutoutComplex;

    public override Graphic? GraphicFor(Pawn pawn)
    {
        string? texPath = TexPathFor(pawn);
        if (texPath.NullOrEmpty())
            return null;
        Shader shader = ShaderFor(pawn);
        Color hairColor = pawn.story?.HairColor ?? Color.white;
        return GraphicDatabase.Get<Graphic_Multi>(texPath, shader, Vector2.one, ColorFor(pawn), hairColor);
    }

    public new Shader ShaderFor(Pawn pawn)
    {
        if (props.shaderTypeDef?.Shader != null)
            return props.shaderTypeDef.Shader;
        if (pawn.Drawer.renderer.StatueColor.HasValue)
            return DefaultShader;
        return props.useSkinShader ? GetSkinShader(pawn) : DefaultShader;
    }

    public static Shader GetSkinShader(Pawn pawn)
    {
        foreach (Hediff hd in pawn.health.hediffSet.hediffs)
        {
            if (hd.def.skinShader != null)
                return hd.def.skinShader.Shader;
        }

        bool dead = pawn.Dead || pawn.mutant is { HasTurned: true, Def.useCorpseGraphics: true };
        return GetSkinShaderAbstract(pawn.story is { SkinColorOverriden: true }, dead);
    }

    public static Shader GetSkinShaderAbstract(bool skinColorOverriden, bool dead)
    {
        if (skinColorOverriden || dead)
            return ShaderDatabase.CutoutComplex;
        return XShaderDatabase.CutoutComplexSkin;
    }
}
