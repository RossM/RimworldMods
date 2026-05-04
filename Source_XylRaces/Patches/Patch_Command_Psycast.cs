using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Command_Psycast))]
    public static class Patch_Command_Psycast
    {
        private static readonly InstructionMatcher Fixup_GetPsycastLevel = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel)),
                    AccessTools.Method(typeof(Patch_Command_Psycast), nameof(GetPsylinkLevel_Wrapper))
                    )
            }
        };
        
        [Feature(nameof(Genes.Psycast)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("DisabledCheck")]
        public static IEnumerable<CodeInstruction> DisabledCheck_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetPsycastLevel.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static int GetPsylinkLevel_Wrapper(Command_Psycast __caller, Pawn pawn)
        {
            return pawn.GetPsylinkLevelFor(__caller.Ability.def);
        }
    }
}
