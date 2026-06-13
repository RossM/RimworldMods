namespace XylXenos.Patches;

[HarmonyPatch(typeof(JoyUtility))]
public static class Patch_JoyUtility
{
    [Feature(typeof(Need_Wetness))]
    [InfixPostfix(typeof(JoyUtility), nameof(JoyUtility.EnjoyableOutsideNow), [typeof(Map), typeof(StringBuilder)])]
    [InfixPatch(nameof(JoyUtility.EnjoyableOutsideNow), [typeof(Pawn), typeof(StringBuilder)])]
    public static void EnjoyableOutsideNow_Postfix(Pawn pawn, Map map, ref bool __result)
    {
        if (pawn.needs.TryGetNeed<Need_Wetness>() != null)
        {
            __result = map.gameConditionManager.AllowEnjoyableOutsideNow(map, out var reason);
        }
    }
}
