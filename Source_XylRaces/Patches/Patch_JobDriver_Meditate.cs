using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(JobDriver_Meditate))]
    public static class Patch_JobDriver_Meditate
    {
        private static readonly InstructionMatcher Fixup_MeditationTick = new()
        {
            Rules =
            {
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.HasPsylink)),
                    AccessTools.Method(typeof(Patch_JobDriver_Meditate), nameof(HasPsylink_Wrapper))
                )
            }
        };

        [Feature(nameof(Genes.Psycast)), HarmonyTranspiler, UsedImplicitly, HarmonyPatch("MeditationTick")]
        public static IEnumerable<CodeInstruction> MeditationTick_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_MeditationTick.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        public static bool HasPsylink_Wrapper(Pawn __instance)
        {
            return __instance.HasPsylink || __instance.HasActivePsycastGene();
        }
    }
}
