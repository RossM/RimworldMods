using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(MemoryThoughtHandler))]
    public static class Patch_MemoryThoughtHandler
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(MemoryThoughtHandler.TryGainMemory), [typeof(Thought_Memory), typeof(Pawn)])]
        public static void TryGainMemory_Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn)
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
