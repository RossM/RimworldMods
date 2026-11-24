namespace XylXenos.Patches;

[Patch(typeof(GenHostility))]
public static class Patch_GenHostility
{
    // Note: This patch is performance-sensitive
    [Feature(typeof(GeneCompProperties_DisableHostility))]
    [Postfix]
    [Target(nameof(GenHostility.HostileTo), typeof(Thing), typeof(Thing))]
    public static void HostileTo_Postfix(Thing a, Thing b, ref bool __result)
    {
        if (!__result)
            return;

        if (a.Faction is null || b.Faction is null)
            return;

        if (a.Map?.HostilityOverrideManager is not { } manager)
            return;

        __result = !manager.HostilityDisabled(b, a) && !manager.HostilityDisabled(a, b);
    }
}
