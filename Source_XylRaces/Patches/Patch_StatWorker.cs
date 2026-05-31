using System.Reflection;
using System.Reflection.Emit;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(StatWorker))]
public static class Patch_StatWorker
{
    private static readonly InstructionMatcher Fixup_GetOffsetsAndFactorsExplanation = new()
    {
        LocalTypes =
        {
            typeof(Pawn),
            typeof(PawnCapacityDef),
            typeof(Hediff_SubstituteCapacity),
        },
        Rules =
        {
            new()
            {
                SaveLocals = true,
                Pattern =
                [
                    // Match to find the local "pawn" is stored in
                    new CodeInstruction(OpCodes.Isinst, typeof(Pawn)),
                    CodeInstruction.StoreLocal(0),
                ]
            },

            new()
            {
                Min = 1, Max = 0,
                Mode = InstructionMatcher.OutputMode.InsertAfter,
                Pattern =
                [
                    CodeInstruction.LoadField(typeof(PawnCapacityOffset), nameof(PawnCapacityOffset.capacity)),
                ],
                Output =
                [
                    CodeInstruction.StoreLocal(1),
                    // Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                    // Load pawn
                    CodeInstruction.LoadLocal(0),
                    // Load capacity
                    CodeInstruction.LoadLocal(1),
                    // Load this.stat
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadField(typeof(StatWorker), "stat"),
                    // Call FindHediffFor
                    CodeInstruction.Call(() => Hediff_SubstituteCapacity.FindHediffFor),
                    // Save a copy of the hediff
                    new CodeInstruction(OpCodes.Dup),
                    CodeInstruction.StoreLocal(2),
                    // capacity = ConditionalSetCapacity(foundHediff, capacity);
                    // Load the capacity
                    CodeInstruction.LoadLocal(1),
                    // Call ConditionalSetCapacity (because I don't want to emit an if)
                    CodeInstruction.Call(() => ConditionalSetCapacity),
                ]
            },
            new()
            {
                Min = 1, Max = 1,
                Chained = true,
                Mode = InstructionMatcher.OutputMode.InsertAfter,
                Pattern =
                [
                    // sb.AppendLine(whitespace + "    " + text + ": " + offset.ToStringSign() + text2 + " (" + text3 + ")");
                    CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]),
                ],
                Output =
                [
                    // AppendSubstitutionDescription(sb, whitespace, foundHediff);
                    // sb
                    CodeInstruction.LoadArgument(2),
                    // whitespace
                    CodeInstruction.LoadArgument(4),
                    // foundHediff
                    CodeInstruction.LoadLocal(2),
                    // Call
                    CodeInstruction.Call(() => AppendSubstitutionDescription),
                ],
            },
            new()
            {
                Min = 1, Max = 0,
                Mode = InstructionMatcher.OutputMode.InsertAfter,
                Pattern =
                [
                    CodeInstruction.LoadField(typeof(PawnCapacityFactor), nameof(PawnCapacityFactor.capacity)),
                ],
                Output =
                [
                    CodeInstruction.StoreLocal(1),
                    // Hediff_SubstituteCapacity foundHediff = FindHediffFor(pawn, capacity, stat);
                    // Load pawn
                    CodeInstruction.LoadLocal(0),
                    // Load capacity
                    CodeInstruction.LoadLocal(1),
                    // Load this.stat
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadField(typeof(StatWorker), "stat"),
                    // Call FindHediffFor
                    CodeInstruction.Call(() => Hediff_SubstituteCapacity.FindHediffFor),
                    // Save a copy of the hediff
                    new CodeInstruction(OpCodes.Dup),
                    CodeInstruction.StoreLocal(2),
                    // capacity = ConditionalSetCapacity(foundHediff, capacity);
                    // Load the capacity
                    CodeInstruction.LoadLocal(1),
                    // Call ConditionalSetCapacity (because I don't want to emit an if)
                    CodeInstruction.Call(() => ConditionalSetCapacity),
                ]
            },
            new()
            {
                Min = 1, Max = 1,
                Chained = true,
                Mode = InstructionMatcher.OutputMode.InsertAfter,
                Pattern =
                [
                    // sb.AppendLine(whitespace + "    " + text8 + ": x" + text9 + " (" + text10 + ")");
                    CodeInstruction.Call(typeof(StringBuilder), nameof(StringBuilder.AppendLine), [typeof(string)]),
                ],
                Output =
                [
                    // AppendSubstitutionDescription(sb, whitespace, foundHediff);
                    // sb
                    CodeInstruction.LoadArgument(2),
                    // whitespace
                    CodeInstruction.LoadArgument(4),
                    // foundHediff
                    CodeInstruction.LoadLocal(2),
                    // Call
                    CodeInstruction.Call(() => AppendSubstitutionDescription),
                ],
            }
        }
    };

    [Feature(typeof(Hediff_SubstituteCapacity))]
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
    public static IEnumerable<CodeInstruction> GetOffsetsAndFactorsExplanation_Transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator,
        MethodBase method)
    {
        var instructionsList = new List<CodeInstruction>(instructions);
        Fixup_GetOffsetsAndFactorsExplanation.MatchAndReplace(method, ref instructionsList, generator);
        return instructionsList;
    }

    public static void AppendSubstitutionDescription(
        StringBuilder sb,
        string whitespace,
        Hediff_SubstituteCapacity foundHediff)
    {
        if (foundHediff != null)
            sb.AppendLine($"{whitespace}        {foundHediff.GetDescription()}");
    }

    public static PawnCapacityDef ConditionalSetCapacity(Hediff_SubstituteCapacity foundHediff, PawnCapacityDef capacity)
    {
        if (foundHediff != null)
            capacity = foundHediff.DefExt.substituteCapacity;
        return capacity;
    }

    // Note: this patch is performance-sensitive
    [Feature(typeof(Hediff_SubstituteCapacity))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [InfixPostfix(typeof(PawnCapacityOffset), nameof(PawnCapacityOffset.capacity))]
    [InfixPatch(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
    public static void PawnCapacityOffset_capacity_Postfix(
        PawnCapacityOffset __instance,
        StatWorker __caller,
        StatRequest req,
        ref PawnCapacityDef __result)
    {
        Hediff_SubstituteCapacity foundHediff
            = Hediff_SubstituteCapacity.FindHediffFor(req.Thing as Pawn, __instance.capacity, __caller.stat);
        if (foundHediff != null)
            __result = foundHediff.DefExt.substituteCapacity;
    }

    [Feature(typeof(Hediff_SubstituteCapacity))]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [InfixPostfix(typeof(PawnCapacityFactor), nameof(PawnCapacityFactor.capacity))]
    [InfixPatch(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
    public static void PawnCapacityFactor_capacity_Postfix(
        PawnCapacityFactor __instance,
        StatWorker __caller,
        StatRequest req,
        ref PawnCapacityDef __result)
    {
        Hediff_SubstituteCapacity foundHediff
            = Hediff_SubstituteCapacity.FindHediffFor(req.Thing as Pawn, __instance.capacity, __caller.stat);
        if (foundHediff != null)
            __result = foundHediff.DefExt.substituteCapacity;
    }

    [Feature(typeof(Psycast))]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.ShouldShowFor))]
    public static void ShouldShowFor_Postfix(StatWorker __instance, StatRequest req, ref bool __result)
    {
        if (req.Thing is not Pawn { HasActivePsycastGene: true })
            return;

        if (__instance.stat == StatDefOf.PsychicEntropyRecoveryRate)
            __result = true;
        if (__instance.stat == StatDefOf.PsychicEntropyMax)
            __result = true;
    }
}
