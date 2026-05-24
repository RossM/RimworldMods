using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using Psycast = XylXenos.Genes.Psycast;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Command_Psycast))]
    public static class Patch_Command_Psycast
    {
        [Feature(typeof(Psycast))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
        [InfixPatch("DisabledCheck")]
        public static void GetPsylinkLevel_Postfix(Command_Psycast __caller, Pawn pawn, ref int __result)
        {
            __result = Math.Max(__result, pawn.GetGeneticPsylinkLevelFor(__caller.Ability.def));
        }
    }
}
