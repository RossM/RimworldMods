namespace Xylib.Patches;

[HarmonyPatch]
public static class PatchPsycast
{
    [Feature(nameof(DefModExtension_GeneWithComps.hasPsycast))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [InfixPatch(typeof(Command_Psycast), "DisabledCheck")]
    public static void GetPsylinkLevel_Postfix(Command_Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.Ability.def));
    }

    [Feature(nameof(DefModExtension_GeneWithComps.hasPsycast))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [InfixPatch(typeof(Psycast), "GizmoDisabled")]
    [InfixPatch(typeof(Psycast), "CanCast")]
    public static void GetPsylinkLevel_Postfix(Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.def));
    }
}
