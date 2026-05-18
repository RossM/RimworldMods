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
    [HarmonyPatch(typeof(ThingComp))]
    public static class Patch_ThingComp
    {
        [Feature(typeof(NotificationManager))]
        [HarmonyPostfix]
        [UsedImplicitly]
        [HarmonyPatch(nameof(ThingComp.Initialize))]
        public static void Initialize_Postfix(ThingComp __instance)
        {
            if (__instance is INotificationTarget target)
                target.RegisterWith(NotificationManager.Instance);
        }
    }
}
