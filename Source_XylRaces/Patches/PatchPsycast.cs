using System;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches;

public static class PatchPsycast
{
    [Feature(nameof(DefExt.hasPsycast))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [InfixPatch("DisabledCheck")]
    public static void GetPsylinkLevel_Postfix(Command_Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.Ability.def));
    }

    [Feature(nameof(DefExt.hasPsycast))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [InfixPatch("GizmoDisabled")]
    [InfixPatch("CanCast")]
    public static void GetPsylinkLevel_Postfix(Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.def));
    }
}
