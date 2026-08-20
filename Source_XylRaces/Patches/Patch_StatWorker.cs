namespace XylXenos.Patches;

[HarmonyPatch(typeof(StatWorker))]
public static class Patch_StatWorker
{
    [Feature(typeof(Hediff_SubstituteCapacity))]
    [Postfix]
    [Inner(typeof(StringBuilder), nameof(StringBuilder.AppendLine), typeof(string))]
    [Target(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
    public static void AppendLine_Postfix(
        StringBuilder __instance,
        string whitespace,
        [State] ref Hediff_SubstituteCapacity? foundHediff)
    {
        if (foundHediff != null)
            __instance.AppendLine($"{whitespace}        {foundHediff.GetDescription()}");
        foundHediff = null;
    }

    [Feature(typeof(Hediff_SubstituteCapacity))]
    [Prefix]
    [Inner(typeof(PawnCapacitiesHandler), nameof(PawnCapacitiesHandler.GetLevel))]
    [Target(nameof(StatWorker.GetOffsetsAndFactorsExplanation))]
    public static void GetLevel_Prefix(
        StatRequest req,
        ref PawnCapacityDef capacity,
        StatDef ___stat,
        [State] ref Hediff_SubstituteCapacity? foundHediff)
    {
        var pawn = req.Thing as Pawn;
        foundHediff = Hediff_SubstituteCapacity.FindHediffFor(pawn, capacity, ___stat);
        if (foundHediff != null)
            capacity = foundHediff.DefExt.substituteCapacity;
    }

    [Feature(typeof(Psycast))]
    [Postfix]
    [Target(typeof(StatWorker), nameof(StatWorker.ShouldShowFor))]
    public static void ShouldShowFor_Postfix(StatDef ___stat, StatRequest req, ref bool __result)
    {
        if (req.Thing is not Pawn { HasActivePsycastGene: true })
            return;

        if (___stat == StatDefOf.PsychicEntropyRecoveryRate)
            __result = true;
        if (___stat == StatDefOf.PsychicEntropyMax)
            __result = true;
    }
}
