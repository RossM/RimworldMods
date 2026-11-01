namespace XylXenos.Patches;

[Patch(typeof(JoyGiver))]
public static class Patch_JoyGiver
{
    [Feature(typeof(GeneCompProperties_JoyGiverChances))]
    [Postfix]
    [Target(nameof(JoyGiver.GetChance))]
    public static void GetChance_Postfix(JoyGiver __instance, Pawn pawn, ref float __result)
    {
        if (pawn.GeneTracker_XylXenos?.joyGiverChanceFactors?.TryGetValue(__instance.def, out var factor) is true)
            __result *= factor;
    }
}
