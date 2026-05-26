using System;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnRenderNode))]
    public static class Patch_PawnRenderNode
    {
        // This allows tattoos to draw properly on top of scaleskin
        [Feature("TODO")]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PawnRenderNode.AddChildren))]
        public static void AddChildren_Postfix(PawnRenderNode __instance)
        {
            Array.Sort(__instance.children, (a, b) => (int)Mathf.Sign(a.Props.baseLayer - b.Props.baseLayer));
        }
    }
}
