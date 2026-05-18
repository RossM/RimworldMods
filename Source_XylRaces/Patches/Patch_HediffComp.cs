using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(HediffComp))]
    public static class Patch_HediffComp
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(HediffComp.CompPostPostAdd))]
        public static void CompPostPostAdd_Postfix(HediffComp __instance)
        {
            if (__instance is INotificationTarget target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
