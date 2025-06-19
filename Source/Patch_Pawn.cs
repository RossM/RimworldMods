using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Pawn))]
    public class Patch_Pawn
    {
        [HarmonyPostfix, HarmonyPatch(nameof(Pawn.BodySize), MethodType.Getter)]
        static void BodySize_Postfix(Pawn __instance, ref float __result)
        {
            List<Gene> genesGenesListForReading = __instance.genes?.GenesListForReading;
            if (genesGenesListForReading == null)
                return;

            foreach (var gene in genesGenesListForReading)
            {
                var extension = gene.def.GetModExtension<ModExtension_GeneDef_Pawn>();
                if (extension != null)
                {
                    __result *= extension.bodySizeFactor;
                }
            }

        }

        [HarmonyPostfix, HarmonyPatch(nameof(Pawn.HealthScale), MethodType.Getter)]
        static void HealthScale_Postfix(Pawn __instance, ref float __result)
        {
            List<Gene> genesGenesListForReading = __instance.genes?.GenesListForReading;
            if (genesGenesListForReading == null)
                return;

            foreach (var gene in genesGenesListForReading)
            {
                var extension = gene.def.GetModExtension<ModExtension_GeneDef_Pawn>();
                if (extension != null)
                {
                    __result *= extension.healthScaleFactor;
                }
            }

        }
    }
}
