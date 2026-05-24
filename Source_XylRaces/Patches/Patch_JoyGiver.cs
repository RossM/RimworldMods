using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(JoyGiver))]
    public class Patch_JoyGiver
    {
        [Feature(nameof(GeneDefExt.joyGiverChanceFactors))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(JoyGiver.GetChance))]
        public static void GetChance_Postfix(JoyGiver __instance, Pawn pawn, ref float __result)
        {
            var factor = GeneHelpers.GetJoyFactor(pawn, __instance);
            __result *= factor;
        }
    }
}
