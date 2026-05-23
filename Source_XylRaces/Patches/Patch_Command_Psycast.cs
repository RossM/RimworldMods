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
        [WrappedMember(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
        [InfixPatch("DisabledCheck")]
        public static int GetPsylinkLevel_Wrapper(Command_Psycast __caller, Pawn pawn)
        {
            return pawn.GetPsylinkLevelFor(__caller.Ability.def);
        }
    }
}
