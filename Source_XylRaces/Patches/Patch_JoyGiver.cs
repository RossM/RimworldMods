using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JoyGiver))]
    public class Patch_JoyGiver
    {
        [Feature(nameof(DefOf.XylAquatic)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(JoyGiver.GetChance))]
        public static void GetChance(JoyGiver __instance, Pawn pawn, ref float __result)
        {
            using (new ProfileBlock())
            {
                if (__instance is JoyGiver_GoSwimming && pawn.HasActiveGene(DefOf.XylAquatic))
                    __result *= 5;
            }
        }
    }
}
