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
    [HarmonyPatch(typeof(Gene))]
    public static class Patch_Gene
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(Gene.PostAdd))]
        public static void PostAdd_Postfix(Gene __instance)
        {
            if (__instance is INotificationTarget target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
