using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class CompProperties_PawnRenderProperties : CompProperties
    {
        public CompProperties_PawnRenderProperties()
        {
            compClass = typeof(CompPawn_RenderProperties);
        }
    }

    public class CompPawn_RenderProperties : ThingComp
    {
        public bool hideClothes;
        public bool hideHeadgear;

        public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
        {
            using (new ProfileBlock())
            {
                var comp = pawn.GetComp<CompPawn_RenderProperties>();
                if (comp != null)
                {
                    if (comp.hideClothes)
                        flags &= ~PawnRenderFlags.Clothes;
                    if (comp.hideHeadgear)
                        flags &= ~PawnRenderFlags.Headgear;
                }

                return flags;
            }
        }
    }
}
