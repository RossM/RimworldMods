using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [HarmonyPatch(typeof(Command_Psycast))]
    public class Patch_Command_Psycast
    {
        private static readonly InstructionMatcher FixupGetPsycastLevel = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Replace = true,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel)),
                    ],
                    Output =
                    [
                        // Load this
                        CodeInstruction.LoadArgument(0),
                        // Load this.ability
                        CodeInstruction.LoadField(typeof(Command_Psycast), "ability"),
                        // Load this.ability.def
                        CodeInstruction.LoadField(typeof(Ability), "def"),
                        // Call replacement function
                        CodeInstruction.Call(typeof(Psy), nameof(Psy.GetPsylinkLevelFor)),
                    ]
                }
            }
        };
        
        [HarmonyTranspiler, HarmonyPatch("DisabledCheck")]
        static IEnumerable<CodeInstruction> DisabledCheck_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!FixupGetPsycastLevel.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("XylRacesCore.Patch_StatWorker.GetOffsetsAndFactorsExplanation_Transpiler: {0}", reason));
            return instructionsList;
        }
    }
}
