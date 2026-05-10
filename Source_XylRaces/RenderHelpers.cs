using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore;

public static class RenderHelpers
{
    public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
    {
        using (new ProfileBlock())
        {
            if (pawn.CurJobDef == DefOf.XylTakeShower && !pawn.pather.Moving)
            {
                flags &= ~(PawnRenderFlags.Clothes | PawnRenderFlags.Headgear);
            }

            return flags;
        }
    }
}