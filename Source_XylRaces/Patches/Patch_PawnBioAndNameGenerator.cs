using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(PawnBioAndNameGenerator))]
    public static class Patch_PawnBioAndNameGenerator
    {
        private delegate bool TryGiveSolidBioTo_Fn(Pawn pawn, string requiredLastName, List<BackstoryCategoryFilter> backstoryCategories);

        private static TryGiveSolidBioTo_Fn TryGiveSolidBioTo_Original = AccessTools.MethodDelegate<TryGiveSolidBioTo_Fn>(
            AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo"));

        private static readonly InstructionMatcher Fixup_TryGiveSolidBioTo = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(PawnBioAndNameGenerator), "TryGiveSolidBioTo"),
                    AccessTools.Method(typeof(Patch_PawnBioAndNameGenerator), nameof(TryGiveSolidBioTo_Wrapper))
                )
            }
        };

        [Feature(nameof(XenotypeDefExtension.allowSolidBackstories))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(PawnBioAndNameGenerator.GiveAppropriateBioAndNameTo))]
        public static IEnumerable<CodeInstruction> DisabledCheck_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_TryGiveSolidBioTo.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGiveSolidBioTo_Wrapper(
            Pawn pawn,
            string requiredLastName,
            List<BackstoryCategoryFilter> backstoryCategories,
            XenotypeDef xenotype)
        {
            if (xenotype?.GetModExtension<XenotypeDefExtension>()?.allowSolidBackstories == false)
                return false;
            return TryGiveSolidBioTo_Original(pawn, requiredLastName, backstoryCategories);
        }
    }
}
