using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(ShotReport))]
    public static class Patch_ShotReport
    {
        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(ShotReport.GetTextReadout))]
        public static void GetTextReadout_Postfix(ShotReport __instance, ref string __result)
        {
            if (__instance.target.Thing is Pawn targetPawn) 
            {
                float rangedDodgeChance = CombatHelpers.GetRangedDodgeChance(targetPawn);
                if (rangedDodgeChance > 0)
                {
                    StringBuilder sb = new StringBuilder(__result);
                    sb.AppendLine("   " + CombatHelpers.Defs.XylRangedDodgeChance.LabelCap + ": " + rangedDodgeChance.ToStringPercent());
                    __result = sb.ToString();
                }
            }
        }
    }
}
