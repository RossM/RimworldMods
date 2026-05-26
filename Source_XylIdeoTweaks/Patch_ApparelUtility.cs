using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace Source_XylIdeoTweaks
{
    [HarmonyPatch(typeof(ApparelUtility))]
    public class Patch_ApparelUtility
    {
        private static readonly InstructionMatcher Fixup = new()
        {
            Rules =
            {
                // Change: Nudity requirements only disable noble/role apparel requirements for the gender they apply to
                InfixPatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(IdeoUtility), nameof(IdeoUtility.IdeoPrefersNudity), [typeof(Ideo)]),
                    AccessTools.Method(typeof(Patch_ApparelUtility), nameof(IdeoPrefersNudity_Wrapper))
                )
            }
        };

        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch(nameof(ApparelUtility.IsRequirementActive))]
        public static IEnumerable<CodeInstruction> IsRequirementActive_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IdeoPrefersNudity_Wrapper(Ideo ideo, Pawn pawn)
        {
            return ideo.IdeoPrefersNudityForGender(pawn.gender);
        }
    }
}
