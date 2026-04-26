using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore;

public static class RenderHelpers
{
    [DefOf]
    private static class Defs
    {
        [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
        public static JobDef XylTakeShower;
    }

    public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
    {
        using (new ProfileBlock())
        {
            if (pawn.CurJobDef == Defs.XylTakeShower && !pawn.pather.Moving)
            {
                flags &= ~(PawnRenderFlags.Clothes | PawnRenderFlags.Headgear);
            }

            return flags;
        }
    }
}