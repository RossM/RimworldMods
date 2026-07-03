namespace XylXenos;

[HarmonyPatch(typeof(LovePartnerRelationUtility))]
public static class PatchLovin
{
    [Feature(typeof(GeneCompProperties_Youthful))]
    [InfixPostfix(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeBiologicalYearsFloat))]
    [InfixPatch(typeof(LovePartnerRelationUtility), "LovinMtbSinglePawnFactor")]
    [InfixPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor))]
    public static void AgeBiologicalYearsFloat_Postfix(Pawn ___pawn, ref float __result)
    {
        foreach (var gene in ___pawn.ActiveGenesOfType<GeneWithComps>())
        {
            if (gene.DefExt.CompProps<GeneCompProperties_Youthful>() is not { } props)
                continue;
            __result = Mathf.Min(__result, props.maxAge);
        }
    }

    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [HarmonyPostfix]
    [HarmonyPatch(typeof(LovePartnerRelationUtility), "LovinMtbSinglePawnFactor")]
    public static void LovinMtbSinglePawnFactor_Postfix(Pawn pawn, ref float __result)
    {
        if (ModsConfig.BiotechActive && pawn.genes != null)
        {
            foreach (Gene item in pawn.genes.GenesListForReading)
            {
                __result *= item.def.lovinMTBFactor;
            }
        }

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            HediffComp_GiveLovinMTBFactor hediffComp_GiveLovinMTBFactor = hediff.TryGetComp<HediffComp_GiveLovinMTBFactor>();
            if (hediffComp_GiveLovinMTBFactor != null)
                __result *= hediffComp_GiveLovinMTBFactor.Props.lovinMTBFactor;
        }
    }
}
