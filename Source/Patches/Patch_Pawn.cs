using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn))]
    public static class Patch_Pawn
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
        static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            List<Gene> genesGenesListForReading = __instance.genes?.GenesListForReading;
            if (genesGenesListForReading == null)
                return;

            foreach (var gene in genesGenesListForReading)
            {
                var extension = gene.def.GetModExtension<GeneDefExtension_Pawn>();
                if (extension != null)
                {
                    __result *= extension.bodySizeFactor;
                }
            }
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            List<Gene> genesGenesListForReading = __instance.genes?.GenesListForReading;
            if (genesGenesListForReading == null)
                return;

            foreach (var gene in genesGenesListForReading)
            {
                var extension = gene.def.GetModExtension<GeneDefExtension_Pawn>();
                if (extension != null)
                {
                    __result *= extension.healthScaleFactor;
                }
            }
        }
    }
}
