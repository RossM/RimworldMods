using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    // ReSharper disable once UnusedMember.Global
    public class PawnRenderNode_Wings : PawnRenderNode
    {
        public PawnRenderNode_Wings(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree)
        {
        }

        protected override string TexPathFor(Pawn pawn)
        {
            if (!props.texPaths.NullOrEmpty())
            {
                if (pawn.flight?.Flying == true)
                    return props.texPaths[1];
                else
                    return props.texPaths[0];
            }

            return base.TexPathFor(pawn);
        }
    }
}
