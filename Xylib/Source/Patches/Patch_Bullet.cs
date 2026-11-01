namespace Xylib.Patches;

[Patch(typeof(Bullet))]
internal static class Patch_Bullet
{
    [Feature(nameof(XStatDefOf.XylRangedDodgeChance))]
    [Prefix]
    [Target("Impact")]
    public static void Impact_Prefix(ref Thing? hitThing, bool blockedByShield)
    {
        if (hitThing is not Pawn pawn || blockedByShield)
            return;

        float rangedDodgeChance = PatchHelpers.GetRangedDodgeChance(pawn);
        if (!Rand.Chance(rangedDodgeChance))
            return;

        MoteMaker.ThrowText(pawn.DrawPos, pawn.Map!, "TextMote_Dodge".Translate(), 1.9f);
        hitThing = null;
    }
}
