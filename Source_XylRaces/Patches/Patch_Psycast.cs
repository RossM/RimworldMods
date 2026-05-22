using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Psycast))]
    public static class Patch_Psycast
    {
        [Feature(typeof(Psycast))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel))]
        [InfixPatch("GizmoDisabled")]
        [InfixPatch("CanCast")]
        public static int GetPsylinkLevel_Wrapper(Psycast __caller, Pawn pawn)
        {
            return pawn.GetPsylinkLevelFor(__caller.def);
        }
    }
}
