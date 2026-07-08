namespace Xylib.Patches;

[HarmonyPatch(typeof(Bullet))]
internal static class Patch_Bullet
{
    [Feature(nameof(XStatDefOf.XylRangedDodgeChance))]
    [HarmonyPrefix]
    [HarmonyPatch("Impact")]
    public static void Impact_Prefix(ref Thing? hitThing, bool blockedByShield)
    {
        if (hitThing is Pawn pawn && !blockedByShield)
        {
            float rangedDodgeChance = PatchHelpers.GetRangedDodgeChance(pawn);
            if (Rand.Chance(rangedDodgeChance))
            {
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TextMote_Dodge".Translate(), 1.9f);
                hitThing = null;
            }
        }
    }
}
