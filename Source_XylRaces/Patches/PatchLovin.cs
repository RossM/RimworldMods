namespace XylXenos.Patches;

[HarmonyPatch]
public static class PatchLovin
{

    [Feature(nameof(Config.Feature.Bugfix_Misc))]
    [Postfix]
    [Target(typeof(LovePartnerRelationUtility), "LovinMtbSinglePawnFactor")]
    public static void LovinMtbSinglePawnFactor_Postfix(Pawn pawn, ref float __result)
    {
        if (ModsConfig.BiotechActive && pawn.genes != null)
            foreach (Gene item in pawn.genes.GenesListForReading)
                __result *= item.def.lovinMTBFactor;

        foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
        {
            if (hediff.TryGetComp<HediffComp_GiveLovinMTBFactor>() is { Props.lovinMTBFactor: var factor })
                __result *= factor;
        }
    }

    [Feature(typeof(GeneComp_LoveEuphoria))]
    [Postfix]
    [Target(typeof(LovePartnerRelationUtility), nameof(LovePartnerRelationUtility.GetLovinMtbHours))]
    public static void GetLovinMtbHours_Postfix(Pawn pawn, Pawn partner, ref float __result)
    {
        if (__result <= 0)
            return;

        if (pawn.FirstActiveGeneCompOfType<GeneComp_LoveEuphoria>()?.Props.maxLovinMtbHours is { } max1 and >= 0)
            __result = Mathf.Min(__result, max1);
        if (partner.FirstActiveGeneCompOfType<GeneComp_LoveEuphoria>()?.Props.maxLovinMtbHours is { } max2 and >= 0)
            __result = Mathf.Min(__result, max2);
    }
}
