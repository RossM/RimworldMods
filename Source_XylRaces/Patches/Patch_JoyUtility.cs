namespace XylXenos.Patches;

[HarmonyPatch(typeof(JoyUtility))]
public static class Patch_JoyUtility
{
    [Feature(typeof(Need_Wetness))]
    [InfixPrefix(typeof(JoyUtility), nameof(JoyUtility.EnjoyableOutsideNow), [typeof(Map), typeof(StringBuilder)])]
    [InfixPatch(nameof(JoyUtility.EnjoyableOutsideNow), [typeof(Pawn), typeof(StringBuilder)])]
    public static bool EnjoyableOutsideNow_Prefix(Pawn pawn, Map map, StringBuilder outFailReason, out bool __result)
    {
        __result = false;

        if (pawn.needs.TryGetNeed<Need_Wetness>() != null)
        {
            __result = map.gameConditionManager.AllowEnjoyableOutsideNow(map, out var reason);
            if (!__result)
                outFailReason?.Append(reason.label);
            return false;
        }

        return true;
    }
}
