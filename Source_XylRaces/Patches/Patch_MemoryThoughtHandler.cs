using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(MemoryThoughtHandler))]
    public static class Patch_MemoryThoughtHandler
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(MemoryThoughtHandler.TryGainMemory), [typeof(Thought_Memory), typeof(Pawn)])]
        public static void TryGainMemory_Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn)
        {
            using (new ProfileBlock())
            {
                var extension = newThought.def.GetModExtension<ThoughtDefExtension_Memory>();
                if (extension?.extraThoughts == null)
                    return;
                foreach (var thoughtDef in extension.extraThoughts)
                {
                    __instance.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, newThought.sourcePrecept), otherPawn);
                }
            }
        }
    }
}
