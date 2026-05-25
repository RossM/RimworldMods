using System;
using RimWorld;
using TranspilerUtil;
using Verse;
using Psycast = XylXenos.Genes.Psycast;

namespace XylXenos.Patches;

public static class PatchPsycast
{
    [Feature(typeof(Psycast))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [InfixPatch("DisabledCheck")]
    public static void GetPsylinkLevel_Postfix(Command_Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.Ability.def));
    }

    [Feature(typeof(RimWorld.Psycast))]
    [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
    [InfixPatch("GizmoDisabled")]
    [InfixPatch("CanCast")]
    public static void GetPsylinkLevel_Postfix(RimWorld.Psycast __caller, Pawn pawn, ref int __result)
    {
        __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.def));
    }
}
