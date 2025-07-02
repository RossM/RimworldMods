using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class Comp_RenderProperties : ThingComp
    {
        public bool hideClothes;
        public bool hideHeadgear;

        public static PawnRenderFlags ModifyRenderFlags(Pawn pawn, PawnRenderFlags flags)
        {
            var comp = pawn.GetComp<Comp_RenderProperties>();
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
