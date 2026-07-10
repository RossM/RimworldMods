using System.Reflection;
using System.Reflection.Emit;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(ITab_Pawn_Visitor))]
public static class Patch_ITab_Pawn_Visitor
{
    private static readonly InstructionMatcher Fixup = new()
    {
        Rules =
        {
            InfixPatcher.MakeRedirectRule(
                AccessTools.Method(typeof(StatWorker_SuppressionFallRate),
                    nameof(StatWorker_SuppressionFallRate.GetExplanationForTooltip)),
                AccessTools.Method(typeof(StatWorker_SuppressionFallRate_Fixed),
                    nameof(StatWorker_SuppressionFallRate_Fixed.GetExplanationForTooltip))),
            new()
            {
                Min = 1, Max = 0,
                Mode = InstructionMatcher.OutputMode.Replace,
                Pattern =
                [
                    new(OpCodes.Castclass, typeof(StatWorker_SuppressionFallRate)),
                ],
                Output =
                [
                    new(OpCodes.Castclass, typeof(StatWorker_SuppressionFallRate_Fixed)),
                ]
            }
        }
    };

    [Feature(nameof(StatDefOf.SlaveSuppressionFallRate))]
    [HarmonyTranspiler]
    [HarmonyPatch("DoSlaveTab")]
    public static IEnumerable<CodeInstruction> DoSlaveTab_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        Fixup.MatchAndReplace(method, ref instructionsList, generator);
        return instructionsList;
    }
}
