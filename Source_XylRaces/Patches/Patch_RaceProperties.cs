using System.Reflection;
using System.Reflection.Emit;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(RaceProperties))]
public static class Patch_RaceProperties
{
    private static readonly InstructionMatcher Fixup_NutritionEatenPerDayExplanation = new()
    {
        Rules =
        {
            new()
            {
                Min = 1, Max = 0,
                Mode = InstructionMatcher.OutputMode.InsertBefore,
                Pattern =
                [
                    // stringBuilder.AppendLine("StatsReport_FinalValue" ...
                    CodeInstruction.LoadLocal(0),
                    new CodeInstruction(OpCodes.Ldstr, "StatsReport_FinalValue"),
                ],
                Output =
                [
                    // LactationExplanation(stringBuilder, pawn);
                    CodeInstruction.LoadLocal(0),
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.Call(() => AddLactationExplanation),
                ]
            },
        }
    };

    [Feature(Config.Feature.Bugfix_Lactation)]
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(RaceProperties), "NutritionEatenPerDayExplanation")]
    public static IEnumerable<CodeInstruction> NutritionEatenPerDayExplanation_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        Fixup_NutritionEatenPerDayExplanation.MatchAndReplace(method, ref instructionsList, generator);
        return instructionsList;
    }

    [Feature(Config.Feature.Bugfix_Lactation)]
    [InfixPrefix(typeof(HediffSet), nameof(HediffSet.GetFirstHediffOfDef))]
    [InfixPatch(typeof(RaceProperties), "NutritionEatenPerDayExplanation")]
    public static bool GetFirstHediffOfDef_Prefix(HediffSet __instance, out Hediff __result)
    {
        __result = null;
        // See comment in Patch_RaceProperties. There is a bug around lactation nutrition in the base game which causes
        // lactating pawns to need too much food. This turns out to be a problem for bossaps balance-wise, so I'm
        // fixing the bug.
        return !Settings.instance.ShouldFixLactationBugsFor(__instance.pawn);
    }

    [Feature(Config.Feature.Bugfix_Lactation)]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(RaceProperties.NutritionEatenPerDay))]
    private static bool NutritionEatenPerDay_Prefix(Pawn p, out string __result)
    {
        __result = null;
        if (!Settings.instance.ShouldFixLactationBugsFor(p))
            return true;

        // There is a bug in the base game that causes the nutrition from lactation to be counted twice, once as part of
        // NutritionEatenPerDay which is used to calculate food fall per tick, and then the lactation hediff itself also
        // directly consumes food per tick. This correctly displays that effect.
        float lactationNutritionUsed = p.LactationHediff?.TryGetComp<HediffComp_Lactating>()?.AddedNutritionPerDay() ?? 0;

        __result = (p.needs.food.FoodFallPerTickAssumingCategory(HungerCategory.Fed) * GenDate.TicksPerDay + lactationNutritionUsed)
            .ToString("0.##");

        return false;
    }

    public static void AddLactationExplanation(StringBuilder stringBuilder, Pawn pawn)
    {
        if (!Settings.instance.ShouldFixLactationBugsFor(pawn))
            return;

        if (pawn.LactationHediff?.TryGetComp<HediffComp_Lactating>() is { } hediffComp_Lactating)
        {
            stringBuilder.AppendLine(
                $"{pawn.LactationHediff.LabelBaseCap}: {hediffComp_Lactating.AddedNutritionPerDay().ToStringWithSign()}");
            stringBuilder.AppendLine();
        }
    }
}
