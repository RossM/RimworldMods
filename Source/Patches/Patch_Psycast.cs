using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Psycast))]
    public static class Patch_Psycast
    {
        private static readonly InstructionMatcher Fixup_GetPsycastLevel = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.Call(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel)),
                    ],
                    Output =
                    [
                        // Load this
                        CodeInstruction.LoadArgument(0),
                        // Load this.def
                        CodeInstruction.LoadField(typeof(Ability), "def"),
                        // Call replacement function
                        CodeInstruction.Call(typeof(PsyHelpers), nameof(PsyHelpers.GetPsylinkLevelFor)),
                    ]
                }
            }
        };
        
        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("GizmoDisabled")]
        static IEnumerable<CodeInstruction> GizmoDisabled_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_GetPsycastLevel.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().FullDescription(), reason));
            return instructionsList;
        }

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("CanCast", MethodType.Getter)]
        static IEnumerable<CodeInstruction> CanCast_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            if (!Fixup_GetPsycastLevel.MatchAndReplace(ref instructionsList, out string reason, generator))
                Log.Error(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().FullDescription(), reason));
            return instructionsList;
        }
    }
}
