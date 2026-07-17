namespace XylXenos.Patches;

[HarmonyPatch(typeof(ShotReport))]
public static class Patch_ShotReport
{
    [Feature(nameof(DefOf.XylEcholocation))]
    [Prefix]
    [Target(nameof(ShotReport.HitFactorFromShooter))]
    public static bool HitFactorFromShooter_Prefix(Thing caster, float distance, out float __result)
    {
        __result = 0f;

        if (PatchHelpers.IsUsingEcholocation(caster))
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
    [Postfix]
    [Target(nameof(ShotReport.HitReportFor))]
    public static void HitReportFor_Postfix(Thing caster, ref ShotReport __result)
    {
        if (PatchHelpers.IsUsingEcholocation(caster))
        {
            __result.factorFromCoveringGas = 1f;
        }
    }
}
