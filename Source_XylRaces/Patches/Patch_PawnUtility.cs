using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnUtility))]
    public static class Patch_PawnUtility
    {
        [Feature(typeof(ChemicalDefExtension))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(PawnUtility.CanTakeDrug))]
        public static bool CanTakeDrug_Prefix(Pawn pawn, ThingDef drug, ref bool __result)
        {
            if (pawn.ChemicalIsAllowedByGenes(drug))
                return true;

            __result = false;
            return false;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(ThingDef)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(Pawn), typeof(Thing), typeof(float)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        public static float GetManhunterOnDamageChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnDamageChance() * Find.Storyteller.difficulty.manhunterChanceOnDamageFactor;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(ThingDef)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(Pawn)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static float GetManhunterOnTameFailChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnTameFailChance();
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(RaceProperties), nameof(RaceProperties.manhunterOnDamageChance))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        public static float RaceProperties_manhunterOnDamageChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnDamageChance();
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(RaceProperties), nameof(RaceProperties.manhunterOnTameFailChance))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static float RaceProperties_manhunterOnTameFailChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnTameFailChance();
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [WrappedMember(typeof(Def), nameof(Def.LabelCap))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static TaggedString Def_LabelCap_Wrapper(Def __instance, Pawn pawn)
        {
            return __instance == pawn.def && pawn.HasActiveGeneDefExtensionOfType<GeneDefExtension_WildMan>()
                ? pawn.genes.XenotypeLabelCap
                : __instance.LabelCap;
        }
    }
}
