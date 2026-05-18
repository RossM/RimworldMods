using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(JoyGiver))]
    public class Patch_JoyGiver
    {
        [Feature(typeof(GeneDefExtension_JoyGivers))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(JoyGiver.GetChance))]
        public static void GetChance(JoyGiver __instance, Pawn pawn, ref float __result)
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
