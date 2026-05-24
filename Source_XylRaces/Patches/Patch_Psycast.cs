using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Psycast))]
    public static class Patch_Psycast
    {
        [Feature(typeof(Psycast))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
        [InfixPatch("GizmoDisabled")]
        [InfixPatch("CanCast")]
        public static void GetPsylinkLevel_Postfix(Psycast __caller, Pawn pawn, ref int __result)
        {
            __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.def));
        }
    }
}
