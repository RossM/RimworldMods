using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(MemoryThoughtHandler))]
    public static class Patch_MemoryThoughtHandler
    {
        [Feature(typeof(ThoughtDefExtension_Memory))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(MemoryThoughtHandler.TryGainMemory), typeof(Thought_Memory), typeof(Pawn))]
        public static void TryGainMemory_Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn)
        {
            var extension = newThought.def.GetModExtension<ThoughtDefExtension_Memory>();
            if (extension?.extraThoughts == null)
                return;
            foreach (var thoughtDef in extension.extraThoughts)
            {
                if (thoughtDef.stages[newThought.CurStageIndex] != null)
                    __instance.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, newThought.CurStageIndex), otherPawn);
            }
        }
    }
}
