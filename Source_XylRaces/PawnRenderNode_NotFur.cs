using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class PawnRenderNode_NotFur(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree)
        : PawnRenderNode(pawn, props, tree)
    {
        public override Graphic GraphicFor(Pawn pawn)
        {
            if (pawn.story?.furDef == null)
            {
                return null;
            }
            return GraphicDatabase.Get<Graphic_Multi>(pawn.story?.furDef.GetFurBodyGraphicPath(pawn), ShaderFor(pawn), Vector2.one, ColorFor(pawn));
        }
    }
}
