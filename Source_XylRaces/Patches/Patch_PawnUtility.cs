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
        [HarmonyPostfix]
        [HarmonyPatch(nameof(PawnUtility.CanTakeDrug))]
        public static void CanTakeDrug_Postfix(Pawn pawn, ThingDef drug, ref bool __result)
        {
            if (!pawn.ChemicalIsAllowedByGenes(drug))
                __result = false;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(ThingDef)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(Pawn), typeof(Thing), typeof(float)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        public static void GetManhunterOnDamageChance_Postfix(Pawn pawn, ref float __result)
        {
            if (pawn.RaceManhunterOnDamageChance() is { } f)
                __result = f * Find.Storyteller.difficulty.manhunterChanceOnDamageFactor;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [InfixPostfix(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(ThingDef)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(Pawn)])]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static void GetManhunterOnTameFailChance_Postfix(Pawn pawn, ref float __result)
        {
            if (pawn.RaceManhunterOnTameFailChance() is { } f)
                __result = f;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [InfixPostfix(typeof(RaceProperties), nameof(RaceProperties.manhunterOnDamageChance))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        public static void RaceProperties_manhunterOnDamageChance_Postfix(Pawn pawn, ref float __result)
        {
            if (pawn.RaceManhunterOnDamageChance() is { } f)
                __result = f;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [InfixPostfix(typeof(RaceProperties), nameof(RaceProperties.manhunterOnTameFailChance))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static void RaceProperties_manhunterOnTameFailChance_Postfix(Pawn pawn, ref float __result)
        {
            if (pawn.RaceManhunterOnTameFailChance() is { } f)
                __result = f;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [InfixPostfix(typeof(Def), nameof(Def.LabelCap))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        [InfixPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static void Def_LabelCap_Postfix(Def __instance, Pawn pawn, ref TaggedString __result)
        {
            if (__instance == pawn.def && pawn.HasActiveGeneDefExtensionOfType<GeneDefExtension_WildMan>())
                __result = pawn.genes.XenotypeLabelCap;
        }
    }
}
