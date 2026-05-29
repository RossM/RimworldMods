namespace XylXenos.Patches;

[HarmonyPatch(typeof(GenHostility))]
public static class Patch_GenHostility
{
    // Note: This patch is performance-sensitive
    [Feature(nameof(DefExt.disableHostilityFromFactions))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GenHostility.HostileTo), typeof(Thing), typeof(Thing))]
    public static void HostileTo_Postfix(Thing a, Thing b, ref bool __result)
    {
        if (!__result)
            return;

        if (a.Faction == null || b.Faction == null)
            return;

        if ((a as Pawn)?.kindDef.hostileToAll == true || (b as Pawn)?.kindDef.hostileToAll == true)
            return;

        var manager = HostilityOverrideManager.GetManager(a.Map);
        if (manager == null)
            return;

        __result = !manager.HostilityDisabled(b, a) && !manager.HostilityDisabled(a, b);
    }
}