using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class PawnRenderNode_Wings(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : PawnRenderNode(pawn, props, tree)
    {
        protected override string TexPathFor(Pawn pawn)
        {
            if (!props.texPaths.NullOrEmpty())
            {
                return pawn.flight?.Flying == true ? props.texPaths[1] : props.texPaths[0];
            }

            return base.TexPathFor(pawn);
        }
    }
}
