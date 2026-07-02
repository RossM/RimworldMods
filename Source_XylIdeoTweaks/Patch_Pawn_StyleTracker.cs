using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Xylib;

namespace Source_XylIdeoTweaks;

[HarmonyPatch(typeof(Pawn_StyleTracker))]
public class Patch_Pawn_StyleTracker
{
    [Feature(Features.PreventRestyleLoops)]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_StyleTracker.HasUnwantedBeard), MethodType.Getter)]
    public static void HasUnwantedBeard_Postfix(Pawn_StyleTracker __instance, ref bool __result)
    {
        if (__result)
        {
            // Only want a style change if there are any valid options to change to
            __result = DefDatabase<BeardDef>.AllDefs.Any(beardDef => PawnStyleItemChooser.WantsToUseStyle(__instance.pawn, beardDef));
        }
    }

    [Feature(Features.PreventRestyleLoops)]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn_StyleTracker.HasUnwantedHairStyle), MethodType.Getter)]
    public static void HasUnwantedHairStyle_Postfix(Pawn_StyleTracker __instance, ref bool __result)
    {
        if (__result)
        {
            // Only want a style change if there are any valid options to change to
            __result = DefDatabase<HairDef>.AllDefs.Any(hairDef => PawnStyleItemChooser.WantsToUseStyle(__instance.pawn, hairDef));
        }
    }
}
