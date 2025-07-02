using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;


namespace XylRacesCore
{
    [HarmonyPatch(typeof(PawnRenderer))]
    public static class Patch_PawnRenderer
    {
        private static readonly InstructionMatcher Fixup_ParallelGetPreRenderResults = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertAfter,
                    Pattern =
                    [
                        // pawnRenderFlags = DefaultRenderFlagsNow | PawnRenderFlags.Clothes | PawnRenderFlags.Headgear;
                        CodeInstruction.Call(typeof(PawnRenderer), "get_DefaultRenderFlagsNow"),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 64),
                        new CodeInstruction(OpCodes.Or),
                        new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                        new CodeInstruction(OpCodes.Or),
                        CodeInstruction.StoreLocal(0),
                    ],
                    Output =
                    [
                        // pawnRenderFlags = Comp_RenderProperties.ModifyRenderFlags(pawn, pawnRenderFlags);
                        // Load this
                        CodeInstruction.LoadArgument(0),
                        // Load this.pawn
                        CodeInstruction.LoadField(typeof(PawnRenderer), "pawn"),
                        // Load pawnRenderFlags
                        CodeInstruction.LoadLocal(0),
                        // Get modified flags
                        CodeInstruction.Call(typeof(CompPawn_RenderProperties), nameof(CompPawn_RenderProperties.ModifyRenderFlags)),
                        // Save pawnRenderFlags
                        CodeInstruction.StoreLocal(0),
                    ]
                }
            }
        };

        [HarmonyTranspiler, HarmonyPatch("ParallelGetPreRenderResults")]
        public static IEnumerable<CodeInstruction> ParallelGetPreRenderResults_Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_ParallelGetPreRenderResults.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().FullDescription(), reason));
            return instructionsList;
        }
    }
}
