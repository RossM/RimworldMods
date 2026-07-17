namespace XylXenos.Patches;

[HarmonyPatch(typeof(JoyGiver))]
public static class Patch_JoyGiver
{
    [Feature(typeof(GeneCompProperties_JoyGiverChances))]
    [Postfix]
    [Target(nameof(JoyGiver.GetChance))]
    public static void GetChance_Postfix(JoyGiver __instance, Pawn pawn, ref float __result)
    {
        var factor = PatchHelpers.GetJoyFactor(pawn, __instance);
        __result *= factor;
    }
}
