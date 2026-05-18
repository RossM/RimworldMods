using Verse;

namespace XylXenos;

public static class RenderHelpers
{
    public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
    {
        if (pawn.CurJobDef == DefOf.XylTakeShower && !pawn.pather.Moving)
        {
            flags &= ~(PawnRenderFlags.Clothes | PawnRenderFlags.Headgear);
        }

        return flags;
    }
}
