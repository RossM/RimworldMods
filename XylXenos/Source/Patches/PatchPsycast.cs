namespace XylXenos.Patches;

[HarmonyPatch]
internal static class PatchPsycast
{
    [Feature(typeof(GeneCompProperties_Psycast))]
    [Postfix]
    [Inner(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [Target(typeof(Command_Psycast), "DisabledCheck")]
    public static void GetPsylinkLevel_Postfix(Command_Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.Ability.def));
    }

    [Feature(typeof(GeneCompProperties_Psycast))]
    [Postfix]
    [Inner(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [Target(typeof(Psycast), "GizmoDisabled")]
    [Target(typeof(Psycast), "CanCast")]
    public static void GetPsylinkLevel_Postfix(Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.def));
    }
}
