// ReSharper disable ForCanBeConvertedToForeach

namespace Xylib.Patches;

[HarmonyPatch]
internal static class PatchPawnRender
{
    // You might think that patching PawnRenderNodeWorker.AppendDrawRequests would work for hiding a single node, but
    // PawnRenderNode.AppendRequests actually checks whether any draw requests were added, and if they weren't skips
    // visiting children. So we're stuck waiting for the complete list to be filled and then removing the ones we don't
    // want afterward.
    [Feature(typeof(GeneCompProperties_RenderNodeModifiers))]
    [Postfix] [Inner(typeof(PawnRenderNode), nameof(PawnRenderNode.AppendRequests))]
    [Target(typeof(PawnRenderTree), nameof(PawnRenderTree.ParallelPreDraw))]
    public static void AppendRequests_Postfix(PawnRenderNode __instance, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
    {
        var geneTracker = parms.pawn?.GeneTracker_Xylib;
        if (geneTracker == null)
            return;

        const RenderNodeModifierType type = RenderNodeModifierType.VisibilitySelfOnly;
        List<RenderNodeModifier>? renderNodeModifiers = geneTracker.renderNodeModifiersByType[(int)type];
        if (renderNodeModifiers is not { Count: > 0 })
            return;

        var count = renderNodeModifiers.Count;

        for (int i = requests.Count - 1; i >= 0; i--)
        {
            bool hidden = false;

            var request = requests[i];
            DebugAssert.NotNull(request);

            for (var j = 0; j < count; j++)
            {
                RenderNodeModifier? renderNodeModifier = renderNodeModifiers[j];
                DebugAssert.NotNull(renderNodeModifier);

                if (renderNodeModifier.Matches(request.node))
                {
                    hidden = true;
                    break;
                }
            }

            if (hidden)
                requests.RemoveAt(i);
        }
    }

    [Feature(typeof(GeneCompProperties_RenderNodeModifiers))]
    [Prefix]
    [Target(typeof(PawnRenderNode), nameof(PawnRenderNode.AppendRequests))]
    public static bool AppendRequests_Prefix(PawnRenderNode __instance, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
    {
        var geneTracker = parms.pawn?.GeneTracker_Xylib;
        if (geneTracker == null)
            return true;

        const RenderNodeModifierType type = RenderNodeModifierType.VisibilitySelfAndChildren;
        List<RenderNodeModifier>? renderNodeModifiers = geneTracker.renderNodeModifiersByType[(int)type];
        if (renderNodeModifiers is not { Count: > 0 })
            return true;

        var count = renderNodeModifiers.Count;
        for (var i = 0; i < count; i++)
        {
            RenderNodeModifier? renderNodeModifier = renderNodeModifiers[i];
            DebugAssert.NotNull(renderNodeModifier);

            if (renderNodeModifier.Matches(__instance))
                return false;
        }

        return true;
    }

    [Feature(typeof(GeneCompProperties_RenderNodeModifiers))]
    [Postfix] [Inner(typeof(PawnRenderNode), nameof(PawnRenderNode.GetTransform))]
    [Target(typeof(PawnRenderTree), nameof(PawnRenderTree.TryGetMatrix))]
    public static void GetTransform_Postfix(
        PawnRenderNode __instance,
        PawnRenderNode node,
        PawnDrawParms parms,
        ref Vector3 offset,
        ref Vector3 scale)
    {
        var geneTracker = parms.pawn?.GeneTracker_Xylib;
        if (geneTracker == null)
            return;

        RenderNodeModifierType type = __instance == node ? RenderNodeModifierType.PositionSelf : RenderNodeModifierType.PositionChildren;
        List<RenderNodeModifier>? renderNodeModifiers = geneTracker.renderNodeModifiersByType[(int)type];
        if (renderNodeModifiers is not { Count: > 0 })
            return;

        var count = renderNodeModifiers.Count;
        for (var i = 0; i < count; i++)
        {
            RenderNodeModifier? renderNodeModifier = renderNodeModifiers[i];
            DebugAssert.NotNull(renderNodeModifier);

            if (renderNodeModifier.Matches(__instance))
            {
                offset += renderNodeModifier.offset;
                scale *= renderNodeModifier.scale;
            }
        }
    }
}
