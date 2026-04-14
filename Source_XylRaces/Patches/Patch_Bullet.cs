using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Bullet))]
    public static class Patch_Bullet
    {
        // It might look better to hook into Projectile instead so we can show a dodge message.
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Bullet.Impact))]
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
