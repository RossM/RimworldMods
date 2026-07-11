namespace Xylib;

internal enum RenderNodeModifierType
{
    PositionSelf,
    PositionChildren,
    VisibilitySelfOnly,
    VisibilitySelfAndChildren,
}

[PublicAPI]
public class RenderNodeModifier
{
    public required PawnRenderNodeTagDef tag;
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
[PublicAPI]
public class GeneCompProperties_RenderNodeModifiers : GeneCompProperties
{
    /// <summary>
    ///     Modifiers to the scale and offset to specific nodes in the pawn's render tree, used to
    ///     change the pawn's visual in a different way than just adding additional nodes.
    /// </summary>
    public required List<RenderNodeModifier> renderNodeModifiers;

    internal List<RenderNodeModifier> RenderNodeModifiersOfType(RenderNodeModifierType type)
    {
        List<RenderNodeModifier> list = [];

        switch (type)
        {
            case RenderNodeModifierType.PositionSelf:
                foreach (RenderNodeModifier m in renderNodeModifiers)
                {
                    if (m.offset != Vector3.zero || m.scale != 1f)
                        list.Add(m);
                }

                return list;

            case RenderNodeModifierType.PositionChildren:
                foreach (RenderNodeModifier m in renderNodeModifiers)
                {
                    if ((m.offset != Vector3.zero || m.scale != 1f) && m.includeChildren)
                        list.Add(m);
                }

                return list;

            case RenderNodeModifierType.VisibilitySelfOnly:
                foreach (RenderNodeModifier m in renderNodeModifiers)
                {
                    if (m.hidden && !m.includeChildren)
                        list.Add(m);
                }

                return list;

            case RenderNodeModifierType.VisibilitySelfAndChildren:
                foreach (RenderNodeModifier m in renderNodeModifiers)
                {
                    if (m.hidden && m.includeChildren)
                        list.Add(m);
                }

                return list;

            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (renderNodeModifiers is null)
            yield break;

        foreach (var modifier in renderNodeModifiers)
        {
            if (modifier.tag is null)
                yield return $"null {nameof(modifier.tag)} in {nameof(renderNodeModifiers)}";
        }
    }
}
