using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Bullet))]
    public static class Patch_Bullet
    {
        [Feature(nameof(DefOf.XylRangedDodgeChance))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Bullet.Impact))]
        public static void Impact_Prefix(Bullet __instance, ref Thing hitThing, bool blockedByShield)
        {
            if (hitThing is Pawn pawn && !blockedByShield)
            {
                float rangedDodgeChance = CombatHelpers.GetRangedDodgeChance(pawn);
                if (Rand.Chance(rangedDodgeChance))
                {
                    MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, "TextMote_Dodge".Translate(), 1.9f);
                    hitThing = null;
                }
            }
        }
    }
}
