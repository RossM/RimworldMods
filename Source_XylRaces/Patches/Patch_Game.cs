using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(Game))]
    public static class Patch_Game
    {
        [Feature(typeof(GeneSet))]
        [HarmonyPostfix]
        [HarmonyPatch(nameof(Game.Dispose))]
        public static void Dispose_Postfix()
        {
            NotificationManager.Instance.Notify(NotificationEvent.PostGameDispose, null);
        }
    }
}
