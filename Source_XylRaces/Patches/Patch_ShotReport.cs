using HarmonyLib;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(ShotReport))]
    public static class Patch_ShotReport
    {
        [DefOf]
        public static class Defs
        {
            [UsedImplicitly] public static GeneDef XylEcholocation;
        }

        [Feature(nameof(Defs.XylEcholocation)), HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(ShotReport.HitFactorFromShooter))]
        public static bool HitFactorFromShooter_Prefix(Thing caster, float distance, float? acc, ref float __result)
        {
            using (new ProfileBlock())
            {
                if (IsUsingEcholocation(caster))
                {
                    float f = acc ?? ((caster is Pawn)
                        ? caster.GetStatValue(StatDefOf.ShootingAccuracyPawn)
                        : (caster?.GetStatValue(StatDefOf.ShootingAccuracyTurret) ?? 1f));
                    float num = Mathf.Pow(f, distance);
                    __result = Mathf.Max(num, 0.0201f);
                    return false;
                }
            }

            return true;
        }

        [Feature(nameof(Defs.XylEcholocation)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(ShotReport.HitReportFor))]
        public static void HitReportFor_Postfix(Thing caster, Verb verb, LocalTargetInfo target, ref ShotReport __result)
        {
            using (new ProfileBlock())
            {
                if (IsUsingEcholocation(caster))
                {
                    __result.factorFromCoveringGas = 1f;
                }
            }
        }

        private static bool IsUsingEcholocation(Thing caster)
        {
            return caster is Pawn pawn && pawn.HasActiveGene(Defs.XylEcholocation) && PawnUtility.IsBiologicallyOrArtificiallyBlind(pawn)
                && pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= 0.2f;
        }

        [Feature(nameof(CombatHelpers.Defs.XylRangedDodgeChance)), HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(ShotReport.GetTextReadout))]
        public static void GetTextReadout_Postfix(ShotReport __instance, ref string __result)
        {
            using (new ProfileBlock())
            {
                if (__instance.target.Thing is Pawn targetPawn)
                {
                    float rangedDodgeChance = CombatHelpers.GetRangedDodgeChance(targetPawn);
                    if (rangedDodgeChance > 0)
                    {
                        StringBuilder sb = new StringBuilder(__result);
                        sb.AppendLine("   " + CombatHelpers.Defs.XylRangedDodgeChance.LabelCap + ": " +
                                      rangedDodgeChance.ToStringPercent());
                        __result = sb.ToString();
                    }
                }
            }
        }
    }
}
