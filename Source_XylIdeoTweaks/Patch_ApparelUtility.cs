using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
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
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(IdeoUtility), nameof(IdeoUtility.IdeoPrefersNudity), [typeof(Ideo)]), 
                    ],
                    Output =
                    [
                        CodeInstruction.LoadArgument(2),
                        CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.gender)), 
                        CodeInstruction.Call(typeof(IdeoUtility), nameof(IdeoUtility.IdeoPrefersNudityForGender), [typeof(Ideo), typeof(Gender)]),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch(nameof(ApparelUtility.IsRequirementActive))]
        public static IEnumerable<CodeInstruction> IsRequirementActive_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }
    }
}
