namespace Xylib.Patches;

[HarmonyPatch(typeof(ShotReport))]
internal static class Patch_ShotReport
{
    [Feature(nameof(XStatDefOf.XylRangedDodgeChance))]
    [Postfix]
    [Target(nameof(ShotReport.GetTextReadout))]
    public static void GetTextReadout_Postfix(TargetInfo ___target, ref string __result)
    {
        if (___target.Thing is Pawn targetPawn)
        {
            float rangedDodgeChance = PatchHelpers.GetRangedDodgeChance(targetPawn);
            if (rangedDodgeChance > 0)
            {
                StringBuilder sb = new StringBuilder(__result);
                sb.AppendLine($"   {XStatDefOf.XylRangedDodgeChance.LabelCap}: {rangedDodgeChance.ToStringPercent()}");
                __result = sb.ToString();
            }
        }
    }
}
