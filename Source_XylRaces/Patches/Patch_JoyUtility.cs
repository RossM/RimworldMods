namespace XylXenos.Patches;

[HarmonyPatch(typeof(JoyUtility))]
public static class Patch_JoyUtility
{
    [Feature(typeof(Need_Wetness))]
    [InnerPrefix(typeof(JoyUtility), nameof(JoyUtility.EnjoyableOutsideNow), [typeof(Map), typeof(StringBuilder)])]
    [Target(nameof(JoyUtility.EnjoyableOutsideNow), [typeof(Pawn), typeof(StringBuilder)])]
    public static bool EnjoyableOutsideNow_Prefix(Pawn pawn, Map map, StringBuilder? outFailReason, out bool __result)
    {
        __result = false;

        if (pawn.needs.TryGetNeed<Need_Wetness>() != null)
        {
            var result = map.gameConditionManager.AllowEnjoyableOutsideNow(map, out var reason);
            if (!result)
            {
                DebugAssert.NotNull(reason);
                DebugAssert.NotNull(reason.label);

                outFailReason?.Append(reason.label);
            }

            __result = result;
            return false;
        }

        return true;
    }
}
