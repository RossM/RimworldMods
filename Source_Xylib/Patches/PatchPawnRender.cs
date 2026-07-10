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
    [InfixPostfix(typeof(PawnRenderNode), nameof(PawnRenderNode.AppendRequests))]
    [InfixPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.ParallelPreDraw))]
    public static void AppendRequests_Postfix(PawnRenderNode __instance, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
    {
        var type = RenderNodeModifierType.VisibilitySelfOnly;
        List<RenderNodeModifier>? renderNodeModifiers = parms.pawn?.GeneTracker_Xylib?.renderNodeModifiersByType[(int)type];
        if (renderNodeModifiers == null)
            return;

        for (int i = requests.Count - 1; i >= 0; i--)
        {
            bool hidden = false;

            var request = requests[i];
            DebugAssert.NotNull(request);

            for (var j = 0; j < renderNodeModifiers.Count; j++)
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
    [HarmonyPrefix]
    [HarmonyPatch(typeof(PawnRenderNode), nameof(PawnRenderNode.AppendRequests))]
    public static bool AppendRequests_Prefix(PawnRenderNode __instance, PawnDrawParms parms, List<PawnGraphicDrawRequest> requests)
    {
        var type = RenderNodeModifierType.VisibilitySelfAndChildren;
        List<RenderNodeModifier>? renderNodeModifiers = parms.pawn?.GeneTracker_Xylib?.renderNodeModifiersByType[(int)type];
        if (renderNodeModifiers == null)
            return true;

        for (var i = 0; i < renderNodeModifiers.Count; i++)
        {
            RenderNodeModifier? renderNodeModifier = renderNodeModifiers[i];
            DebugAssert.NotNull(renderNodeModifier);

            if (renderNodeModifier.Matches(__instance))
                return false;
        }

        return true;
    }

    [Feature(typeof(GeneCompProperties_RenderNodeModifiers))]
    [InfixPostfix(typeof(PawnRenderNode), nameof(PawnRenderNode.GetTransform))]
    [InfixPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.TryGetMatrix))]
    public static void GetTransform_Postfix(
        PawnRenderNode __instance,
        PawnRenderNode node,
        PawnDrawParms parms,
        ref Vector3 offset,
        ref Vector3 scale)
    {
        var type = __instance == node ? RenderNodeModifierType.PositionSelf : RenderNodeModifierType.PositionChildren;
        List<RenderNodeModifier>? renderNodeModifiers = parms.pawn?.GeneTracker_Xylib?.renderNodeModifiersByType[(int)type];
        if (renderNodeModifiers == null)
            return;

        for (var i = 0; i < renderNodeModifiers.Count; i++)
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
