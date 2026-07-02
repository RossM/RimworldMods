using HarmonyLib;
using Verse;
using Xylib;

namespace Source_XylIdeoTweaks;

[HarmonyPatch(typeof(Pawn))]
public class Patch_Pawn
{
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Pawn.ExposeData))]
    public static void ExposeData_Postfix(Pawn __instance)
    {
        var pawnData = PawnExtraData<PawnData>.Get(__instance);
        
        Scribe_Deep.Look(ref pawnData, "XylIdeos_PawnData");

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            pawnData.Init(__instance);
            PawnExtraData<PawnData>.Set(__instance, pawnData);
        }
    }
}
