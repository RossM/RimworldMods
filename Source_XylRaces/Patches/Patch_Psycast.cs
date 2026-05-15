using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using TranspilerUtil;
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
                InstructionMatcher.MakeRedirectRule(
                    AccessTools.Method(typeof(PawnUtility), nameof(PawnUtility.GetPsylinkLevel)),
                    AccessTools.Method(typeof(Patch_Psycast), nameof(GetPsylinkLevel_Wrapper)))
            }
        };

        [Feature(nameof(Psycast))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch("GizmoDisabled")]
        public static IEnumerable<CodeInstruction> GizmoDisabled_Transpiler(IEnumerable<CodeInstruction> instructions,
                                                                            ILGenerator generator,
                                                                            MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetPsycastLevel.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [Feature(nameof(Psycast))]
        [HarmonyTranspiler]
        [UsedImplicitly]
        [HarmonyPatch("CanCast", MethodType.Getter)]
        public static IEnumerable<CodeInstruction> CanCast_Transpiler(IEnumerable<CodeInstruction> instructions,
                                                                      ILGenerator generator,
                                                                      MethodBase method)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetPsycastLevel.MatchAndReplace(method, ref instructionsList, generator);
            return instructionsList;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetPsylinkLevel_Wrapper(Psycast __caller, Pawn pawn)
        {
            return pawn.GetPsylinkLevelFor(__caller.def);
        }
    }
}
