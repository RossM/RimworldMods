namespace XylXenos.Patches;

[Patch(typeof(PawnRenderer))]
public static class Patch_PawnRenderer
{
    [Feature(nameof(DefOf.XylTakeShower))]
    [Prefix]
    [Inner(typeof(PawnRenderer), "GetDrawParms")]
    [Target("ParallelGetPreRenderResults")]
    public static void GetDrawParms_Prefix(Pawn ___pawn, ref PawnRenderFlags flags)
    {
        flags = PatchHelpers.ModifyRenderFlags(___pawn, flags);
    }
}
