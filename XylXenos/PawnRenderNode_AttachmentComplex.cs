namespace XylXenos;

[UsedFromXml]
public class PawnRenderNode_AttachmentComplex(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
    : PawnRenderNode(pawn, props, tree)
{
    public override Graphic? GraphicFor(Pawn pawn)
    {
        string? texPath = TexPathFor(pawn);
        if (texPath.NullOrEmpty())
            return null;
        Shader shader = ShaderFor(pawn);
        Color hairColor = pawn.story?.HairColor ?? Color.white;
        return GraphicDatabase.Get<Graphic_Multi>(texPath, shader, Vector2.one, ColorFor(pawn), hairColor);
    }

    protected override Shader DefaultShader => ShaderDatabase.CutoutComplex;

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

        bool dead = pawn.Dead || (pawn.IsMutant && pawn.mutant.HasTurned && pawn.mutant.Def.useCorpseGraphics);
        return GetSkinShaderAbstract(pawn.story is { SkinColorOverriden: true }, dead);
    }

    public static Shader GetSkinShaderAbstract(bool skinColorOverriden, bool dead)
    {
        if (skinColorOverriden || dead)
            return ShaderDatabase.CutoutComplex;
        return XShaderDatabase.CutoutComplexSkin;
    }
}
