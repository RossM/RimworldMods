namespace Xylib;

[UsedFromXml]
public class GeneCompProperties_RenderNodeModifiers : GeneCompProperties
{
    /// <summary>
    ///     Modifiers to the scale and offset to specific nodes in the pawn's render tree, used to
    ///     change the pawn's visual in a different way than just adding additional nodes.
    /// </summary>
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;
}
