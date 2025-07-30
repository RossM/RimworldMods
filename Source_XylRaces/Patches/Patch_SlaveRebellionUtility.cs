using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(SlaveRebellionUtility))]
    public static class Patch_SlaveRebellionUtility
    {
        public const float DocileFactor = 4f;
        public const float NeverRebelThresholdDays = 120f;

        [DefOf]
        public static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Bossaps")]
            public static GeneDef XylDocile;
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch("InitiateSlaveRebellionMtbDaysHelper")]
        public static void InitiateSlaveRebellionMtbDaysHelper_Postfix(Pawn pawn, ref float __result)
        {
            if (__result < 0)
                return;
            if (pawn.HasActiveGene(Defs.XylDocile))
            {
                __result *= DocileFactor;
                if (__result > NeverRebelThresholdDays)
                    __result = -1;
            }
        }

        private static readonly InstructionMatcher Fixup_GetSlaveRebellionMtbCalculationExplanation = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.InsertAfter,
                    Pattern =
                    [
                        CodeInstruction.LoadLocal(0), 
                        new CodeInstruction(OpCodes.Ldstr, "{0}: {1}"),
                        new CodeInstruction(OpCodes.Ldstr, "SuppressionFinalInterval"),
                    ],
                    Output =
                    [
                        // Load stringBuilder
                        CodeInstruction.LoadLocal(0),
                        // Load pawn
                        CodeInstruction.LoadArgument(0), 
                        // Call AddAdditionalExplanation
                        CodeInstruction.Call(() => AddAdditionalExplanation), 
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
        public static IEnumerable<CodeInstruction> GetSlaveRebellionMtbCalculationExplanation_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_GetSlaveRebellionMtbCalculationExplanation.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch("GetSlaveRebellionMtbCalculationExplanation")]
        public static bool GetSlaveRebellionMtbCalculationExplanation_Prefix(Pawn pawn, ref string __result)
        {
            if (SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(pawn) < 0)
            {
                __result = "";
                return false;
            }

            return true;
        }

        private static void AddAdditionalExplanation(StringBuilder stringBuilder, Pawn pawn)
        {
            if (pawn.HasActiveGene(Defs.XylDocile))
            {
                stringBuilder.AppendLine($"{Defs.XylDocile.LabelCap}: x{DocileFactor.ToStringPercent()}");
            }
        }
    }
}
