using System.Collections.Generic;
using System.Reflection;
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
        public static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            using (new ProfileBlock(MethodBase.GetCurrentMethod(), enabled: false))
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
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        public static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            using (new ProfileBlock(MethodBase.GetCurrentMethod(), enabled: false))
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
}
