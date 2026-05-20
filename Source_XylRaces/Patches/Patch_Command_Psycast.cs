using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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
        private static readonly InstructionMatcher.Rule Rule_GetPsylinkLevel
            = InstructionMatcher.MakeRedirectRule(PawnUtility.GetPsylinkLevel, GetPsylinkLevel_Wrapper);

        [Feature(typeof(Psycast))]
        [HarmonyTranspiler]
        [HarmonyPatch("DisabledCheck")]
        public static IEnumerable<CodeInstruction> DisabledCheck_Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator,
            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            new InstructionMatcher()
            {
                Rules =
                {
                    Rule_GetPsylinkLevel
                }
            }.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPsylinkLevel_Wrapper(Command_Psycast __caller, Pawn pawn)
        {
            return pawn.GetPsylinkLevelFor(__caller.Ability.def);
        }
    }
}
