using System.Collections.Generic;
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
                InstructionMatcher.RedirectMethodRule(
                    AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel)),
                    AccessTools.Method(typeof(Patch_Command_Psycast), nameof(GetPsylinkLevel))
                    )
            }
        };
        
        [Feature(nameof(Genes.Psycast)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("DisabledCheck")]
        public static IEnumerable<CodeInstruction> DisabledCheck_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetPsycastLevel.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        public static int GetPsylinkLevel(Pawn pawn, Command_Psycast instance)
        {
            return pawn.GetPsylinkLevelFor(instance.Ability.def);
        }
    }
}
