using Verse;

namespace XylRacesCore;

public static class RenderHelpers
{
    public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
    {
        using (new ProfileBlock())
        {
            var comp = pawn.GetComp<CompPawn_RenderProperties>();
            if (comp != null)
            {
                if (comp.job != null && comp.job != pawn.CurJob)
                {
                    comp.job = null;
                    comp.hideClothes = comp.hideHeadgear = false;
                }
                    
                if (comp.hideClothes)
                    flags &= ~PawnRenderFlags.Clothes;
                if (comp.hideHeadgear)
                    flags &= ~PawnRenderFlags.Headgear;
            }

            if (pawn.CurJob?.GetCachedDriver(pawn) is JobDriver_TakeShower { showering: true })
            {
                flags &= ~(PawnRenderFlags.Clothes | PawnRenderFlags.Headgear);
            }

            return flags;
        }
    }
}