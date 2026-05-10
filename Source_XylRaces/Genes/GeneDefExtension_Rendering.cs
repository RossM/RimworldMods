using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace XylRacesCore.Genes
{
    public class RenderNodeModifier
    {
        public Type renderNodeClass = null;
        public bool onlyRoot = false;
        public float scale = 1.0f;
        public Vector3 offset = Vector3.zero;

        public bool Matches(PawnRenderNode node)
        {
            if (onlyRoot && node.parent != null)
                return false;
            if (renderNodeClass != null && node.Worker.GetType() != renderNodeClass)
                return false;
            return true;
        }
    }

    public class GeneDefExtension_Rendering : GeneDefExtension
    {
        public List<RenderNodeModifier> modifiers;
    }
}
