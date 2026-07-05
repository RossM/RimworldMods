namespace Xylib;

internal enum RenderNodeModifierType
{
    PositionSelf,
    PositionChildren,
    VisibilitySelfOnly,
    VisibilitySelfAndChildren,
}

public class RenderNodeModifier
{
    public PawnRenderNodeTagDef tag;
    public float scale = 1.0f;
    public Vector3 offset = Vector3.zero;
    public bool hidden = false;
    public bool includeChildren = true;

    public bool Matches(PawnRenderNode node)
    {
        return node.Props.tagDef == tag;
    }
}

[UsedFromXml]
public class GeneCompProperties_RenderNodeModifiers : GeneCompProperties
{
    /// <summary>
    ///     Modifiers to the scale and offset to specific nodes in the pawn's render tree, used to
    ///     change the pawn's visual in a different way than just adding additional nodes.
    /// </summary>
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;

    internal List<RenderNodeModifier> RenderNodeModifiersOfType(RenderNodeModifierType type)
    {
        return type switch
        {
            RenderNodeModifierType.PositionSelf => renderNodeModifiers?
                .Where(m => m.offset != Vector3.zero || m.scale != 1f).ToList(),
            RenderNodeModifierType.PositionChildren => renderNodeModifiers?
                .Where(m => (m.offset != Vector3.zero || m.scale != 1f) && m.includeChildren).ToList(),
            RenderNodeModifierType.VisibilitySelfOnly => renderNodeModifiers?
                .Where(m => m.hidden && !m.includeChildren).ToList(),
            RenderNodeModifierType.VisibilitySelfAndChildren => renderNodeModifiers?
                .Where(m => m.hidden && m.includeChildren).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
