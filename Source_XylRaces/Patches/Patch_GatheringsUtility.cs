namespace XylXenos.Patches;

[HarmonyPatch(typeof(GatheringsUtility))]
public static class Patch_GatheringsUtility
{
    [Feature(nameof(Config.Feature.Joyless))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(GatheringsUtility.ShouldPawnKeepGathering))]
    public static void ShouldPawnKeepGathering_Postfix(Pawn p, GatheringDef gatheringDef, ref bool __result)
    {
        if (gatheringDef.respectTimetable && p.needs.joy == null)
            __result = false;
    }
}
