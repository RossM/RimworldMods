using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JoyGiver))]
    public class Patch_JoyGiver
    {
        [Feature(nameof(GeneDefExtension_JoyGivers)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(JoyGiver.GetChance))]
        public static void GetChance(JoyGiver __instance, Pawn pawn, ref float __result)
        {
            using (new ProfileBlock())
            {
                foreach (var defExt in pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_JoyGivers>())
                {
                    if (defExt.joyGiverChanceFactors.NullOrEmpty())
                        continue;
                    foreach (var joyGiverFactor in defExt.joyGiverChanceFactors)
                    {
                        if (joyGiverFactor.joyGiver == __instance.def) 
                            __result *= joyGiverFactor.factor;
                    }
                }
            }
        }
    }
}
