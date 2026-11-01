namespace XylXenos.Patches;

[Patch(typeof(ShotReport))]
public static class Patch_ShotReport
{
    [Feature(nameof(MyDefOf.XylEcholocation))]
    [Postfix]
    [Target(nameof(ShotReport.HitReportFor))]
    public static void HitReportFor_Postfix(Thing caster, ref ShotReport __result)
    {
        if (PatchHelpers.IsUsingEcholocation(caster))
            __result.factorFromCoveringGas = 1f;
    }
}
