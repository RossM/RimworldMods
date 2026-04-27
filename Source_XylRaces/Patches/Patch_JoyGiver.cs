using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JoyGiver))]
    public class Patch_JoyGiver
    {
        [DefOf]
        private static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")] 
            public static GeneDef XylAquatic;
        }

        [Feature(nameof(Defs.XylAquatic)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(JoyGiver.GetChance))]
        public static void GetChance(JoyGiver __instance, Pawn pawn, ref float __result)
        {
            using (new ProfileBlock())
            {
                if (__instance is JoyGiver_GoSwimming && pawn.HasActiveGene(Defs.XylAquatic))
                    __result *= 5;
            }
        }
    }
}
