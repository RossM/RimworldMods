namespace XylXenos.Patches;

[HarmonyPatch(typeof(ShotReport))]
public static class Patch_ShotReport
{
    [Feature(nameof(DefOf.XylRangedDodgeChance))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShotReport.GetTextReadout))]
    public static void GetTextReadout_Postfix(TargetInfo ___target, ref string __result)
    {
        if (___target.Thing is Pawn targetPawn)
        {
            float rangedDodgeChance = CombatHelpers.GetRangedDodgeChance(targetPawn);
            if (rangedDodgeChance > 0)
            {
                StringBuilder sb = new StringBuilder(__result);
                sb.AppendLine($"   {DefOf.XylRangedDodgeChance.LabelCap}: {rangedDodgeChance.ToStringPercent()}");
                __result = sb.ToString();
            }
        }
    }

    [Feature(nameof(DefOf.XylEcholocation))]
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ShotReport.HitFactorFromShooter))]
    public static bool HitFactorFromShooter_Prefix(Thing caster, float distance, out float __result)
    {
        __result = 0f;

        if (IsUsingEcholocation(caster))
        {
            float shootingAccuracy = caster switch
            {
                Pawn => caster.GetStatValue(StatDefOf.ShootingAccuracyPawn),
                not null => caster.GetStatValue(StatDefOf.ShootingAccuracyTurret),
                _ => 1f,
            };
            float hitFactor = Mathf.Pow(shootingAccuracy, distance);
            __result = Mathf.Max(hitFactor, 0.0201f);
            return false;
        }

        return true;
    }

    [Feature(nameof(DefOf.XylEcholocation))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(ShotReport.HitReportFor))]
    public static void HitReportFor_Postfix(Thing caster, ref float ___factorFromCoveringGas, ref ShotReport __result)
    {
        if (IsUsingEcholocation(caster))
        {
            ___factorFromCoveringGas = 1f;
        }
    }

    private static bool IsUsingEcholocation(Thing caster)
    {
        return caster is Pawn pawn && pawn.HasActiveGene(DefOf.XylEcholocation) && PawnUtility.IsBiologicallyOrArtificiallyBlind(pawn)
               && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= 0.2f;
    }
}
