namespace Xylib.Patches;

[Patch(typeof(ShotReport))]
internal static class Patch_ShotReport
{
    [Feature(nameof(XStatDefOf.XylRangedDodgeChance))]
    [Postfix]
    [Target(nameof(ShotReport.GetTextReadout))]
    public static void GetTextReadout_Postfix(TargetInfo ___target, ref string __result)
    {
        if (___target.Thing is not Pawn targetPawn)
            return;

        float rangedDodgeChance = PatchHelpers.GetRangedDodgeChance(targetPawn);
        if (!(rangedDodgeChance > 0))
            return;

        StringBuilder sb = new StringBuilder(__result);
        sb.AppendLine($"   {XStatDefOf.XylRangedDodgeChance.LabelCap}: {rangedDodgeChance.ToStringPercent()}");
        __result = sb.ToString();
    }
}
