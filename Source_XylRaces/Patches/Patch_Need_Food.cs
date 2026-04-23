using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Need_Food))]
    public static class Patch_Need_Food
    {
        private static readonly InstructionMatcher Fixup_FoodFallPerTickAssumingCategory = new()
        {
            Rules =
            {
                new()
                {
                    Min = 1, Max = 0,
                    Mode = InstructionMatcher.OutputMode.Replace,
                    Pattern =
                    [
                        CodeInstruction.LoadLocal(0),
                        CodeInstruction.Call(typeof(HediffComp_Lactating), nameof(HediffComp_Lactating.AddedNutritionPerDay)),
                    ],
                    Output =
                    [
                        CodeInstruction.LoadArgument(0),
                        CodeInstruction.LoadField(typeof(Need), "pawn"),
                        CodeInstruction.Call(() => GetAddedNutritionPerDay),
                    ]
                }
            }
        };

        [HarmonyTranspiler, UsedImplicitly, HarmonyPatch("FoodFallPerTickAssumingCategory")]
        public static IEnumerable<CodeInstruction> FoodFallPerTickAssumingCategory_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = new List<CodeInstruction>(instructions);
            Fixup_FoodFallPerTickAssumingCategory.MatchAndReplace(ref instructionsList, generator);
            return instructionsList;
        }

        static float GetAddedNutritionPerDay(Pawn pawn)
        {
            return PatchLactation.GetLactationHediffUnlessAddedByGene(pawn)?.TryGetComp<HediffComp_Lactating>()?.AddedNutritionPerDay() ?? 0;
        }
    }
}
