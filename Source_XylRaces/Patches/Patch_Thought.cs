using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Thought))]
    public static class Patch_Thought
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Thought.Description), MethodType.Getter)]
        public static void Description_Postfix(Thought __instance, ref string __result)
        {
            GeneDef sourceGene = __instance.def.requiredGenes.EmptyIfNull().FirstOrDefault(geneDef => __instance.pawn.HasActiveGene(geneDef));
            if (sourceGene == null) 
                return;

            // This is a minor UI improvement to show which gene caused a thought
            __result += "\n\n" + ("IncapableOfTooltipGene".Translate() + ": " + sourceGene.LabelCap).Colorize(ColoredText.GeneColor);
        }
    }
}
