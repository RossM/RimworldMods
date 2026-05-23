using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt))]
    public static class Patch_InteractionWorker_RecruitAttempt
    {
        [Feature(nameof(DefOf.XylResistanceFallRate))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(StatExtension), nameof(StatExtension.GetStatValue))]
        [InfixPatch(nameof(InteractionWorker_RecruitAttempt.Interacted))]
        public static float GetStatValue_Wrapper(Thing thing, StatDef stat, Pawn recipient, bool applyPostProcess, int cacheStaleAfterTicks)
        {
            float value = thing.GetStatValue(stat, applyPostProcess, cacheStaleAfterTicks);
            if (stat == StatDefOf.NegotiationAbility)
                value *= recipient.GetStatValue(DefOf.XylResistanceFallRate);
            return value;
        }
    }
}
