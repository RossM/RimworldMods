namespace XylXenos.Patches;

[Patch(typeof(Alert_NeedJoySources))]
public static class Patch_Alert_NeedJoySources
{
    [Feature(FeatureNameOf.Joyless)]
    [Postfix]
    [Target("NeedJoySource")]
    public static void NeedJoySource_Postfix(Map map, ref bool __result)
    {
        if (!map.mapPawns.FreeColonists.Any(pawn => pawn.needs?.joy != null))
            __result = false;
    }
}
