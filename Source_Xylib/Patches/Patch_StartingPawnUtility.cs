using System.Reflection;
using System.Reflection.Emit;

namespace Xylib.Patches;

[HarmonyPatch(typeof(StartingPawnUtility))]
internal static class Patch_StartingPawnUtility
{
    private static readonly InstructionMatcher.Rule Rule_GetExtraStartingItems = new()
    {
        Min = 1, Max = 1,
        Mode = InstructionMatcher.OutputMode.InsertAfter,
        Pattern =
        [
            CodeInstruction.Call(typeof(Rand), $"get_{nameof(Rand.Value)}"),
        ],
        Output =
        [
            // Load pawn
            CodeInstruction.LoadArgument(0),
            // Call GetExtraStartingItems
            CodeInstruction.Call(() => GetExtraStartingItems),
        ]
    };

    [Feature(nameof(EventDefOf.InGeneratePossessions))]
    [HarmonyTranspiler]
    [HarmonyPatch("GeneratePossessions")]
    public static IEnumerable<CodeInstruction> GeneratePossessions_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        return InstructionMatcher.MatchAndReplace([Rule_GetExtraStartingItems], method, instructions, generator);
    }

    [Feature(typeof(CompProperties_Drug))]
    [InfixPrefix(typeof(List<ThingDefCount>), "Add")]
    [InfixPatch("GeneratePossessions")]
    public static bool List_Add_Prefix(List<ThingDefCount> __instance, ThingDefCount item, Pawn pawn)
    {
        var chemical = item.ThingDef.GetCompProperties<CompProperties_Drug>()?.chemical;
        return chemical == null || pawn.ChemicalIsAllowedByGenes(chemical);
    }

    public static void GetExtraStartingItems(Pawn pawn)
    {
        var items = Find.GameInitData.startingPossessions[pawn];
        EventManager.Instance.Notify(EventDefOf.InGeneratePossessions, pawn, items);
    }
}
