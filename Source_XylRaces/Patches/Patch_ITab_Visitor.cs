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
                SymbolExtensions.GetMethodInfo((StatWorker_SuppressionFallRate o) => o.GetExplanationForTooltip(default)),
                SymbolExtensions.GetMethodInfo((StatWorker_SuppressionFallRate_Fixed o) => o.GetExplanationForTooltip(default))),
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
                ],
            },
        },
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
