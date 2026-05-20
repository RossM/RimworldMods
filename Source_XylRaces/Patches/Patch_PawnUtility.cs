using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnUtility))]
    public static class Patch_PawnUtility
    {
        private static readonly InstructionMatcher.Rule GetManhunterOnDamageChance_Rule = InstructionMatcher.MakeRedirectRule(
            AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(ThingDef)]),
            GetManhunterOnDamageChance_Wrapper);

        private static readonly InstructionMatcher.Rule GetManhunterOnTameFailChance_Rule = InstructionMatcher.MakeRedirectRule(
            AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(ThingDef)]),
            GetManhunterOnTameFailChance_Wrapper);

        private static readonly InstructionMatcher.Rule Def_LabelCap_Rule = InstructionMatcher.MakeRedirectRule(
            AccessTools.PropertyGetter(typeof(Def), nameof(Def.LabelCap)),
            Def_LabelCap_Wrapper);

        private static readonly InstructionMatcher.Rule RaceProperties_manhunterOnDamageChance_Rule = InstructionMatcher.MakeRedirectRule(
            AccessTools.Field(typeof(RaceProperties), nameof(RaceProperties.manhunterOnDamageChance)),
            RaceProperties_manhunterOnDamageChance_Wrapper);

        private static readonly InstructionMatcher.Rule RaceProperties_manhunterOnTameFailChance_Rule = InstructionMatcher.MakeRedirectRule(
            AccessTools.Field(typeof(RaceProperties), nameof(RaceProperties.manhunterOnTameFailChance)),
            RaceProperties_manhunterOnTameFailChance_Wrapper);

        [Feature(typeof(ChemicalDefExtension))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnUtility.CanTakeDrug))]
        public static bool CanTakeDrug_Prefix(Pawn pawn, ThingDef drug, ref bool __result)
        {
            if (pawn.ChemicalIsAllowedByGenes(drug))
                return true;

            __result = false;
            return false;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnUtility.GetManhunterOnDamageChance), [typeof(Pawn), typeof(Thing), typeof(float)])]
        public static IEnumerable<CodeInstruction> GetManhunterOnDamageChance_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    GetManhunterOnDamageChance_Rule,
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnUtility.GetManhunterOnDamageChanceExplanation))]
        public static IEnumerable<CodeInstruction> GetManhunterOnDamageChanceExplanation_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    GetManhunterOnDamageChance_Rule,
                    RaceProperties_manhunterOnDamageChance_Rule,
                    Def_LabelCap_Rule,
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnUtility.GetManhunterOnTameFailChance), [typeof(Pawn)])]
        public static IEnumerable<CodeInstruction> GetManhunterOnTameFailChance_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    GetManhunterOnTameFailChance_Rule,
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(typeof(GeneDefExtension_WildMan))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnUtility.GetManhunterOnTameFailChanceExplanation))]
        public static IEnumerable<CodeInstruction> GetManhunterOnTameFailChanceExplanation_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    GetManhunterOnTameFailChance_Rule,
                    RaceProperties_manhunterOnTameFailChance_Rule,
                    Def_LabelCap_Rule,
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetManhunterOnDamageChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnDamageChance() * Find.Storyteller.difficulty.manhunterChanceOnDamageFactor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetManhunterOnTameFailChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnTameFailChance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RaceProperties_manhunterOnDamageChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnDamageChance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RaceProperties_manhunterOnTameFailChance_Wrapper(Pawn pawn)
        {
            return pawn.RaceManhunterOnTameFailChance();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TaggedString Def_LabelCap_Wrapper(Def __instance, Pawn pawn)
        {
            return __instance == pawn.def && pawn.HasActiveGeneDefExtensionOfType<GeneDefExtension_WildMan>()
                ? pawn.genes.XenotypeLabelCap
                : __instance.LabelCap;
        }
    }
}
