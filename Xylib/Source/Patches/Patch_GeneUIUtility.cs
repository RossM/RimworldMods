namespace Xylib.Patches;

[Patch(typeof(GeneUIUtility))]
// ReSharper disable once InconsistentNaming
internal static class Patch_GeneUIUtility
{
    [Feature(nameof(DefModExtension_GeneWithComps.backgroundPath))]
    [Prefix]
    [Inner(typeof(GUI), nameof(GUI.DrawTexture), typeof(Rect), typeof(Texture))]
    [Target("DrawGeneBasics")]
    public static void DrawTexture_Prefix(GeneDef gene, ref Texture image, [State] ref int callIndex)
    {
        if (callIndex++ > 0)
            return;

        if (gene.Extension_GeneWithComps?.Background is { } background)
            image = background;
    }
}
