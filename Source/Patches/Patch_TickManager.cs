using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(TickManager))]
    public static class Patch_TickManager
    {
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(TickManager.DoSingleTick))]
        public static void DoSingleTick_Prefix()
        {
            if (ProfileBlock.GlobalEnabled)
                DeepProfiler.Start("DoSingleTick");
        }

        [HarmonyPostfix, UsedImplicitly, HarmonyPatch(nameof(TickManager.DoSingleTick))]
        public static void DoSingleTick_Postfix()
        {
            if (ProfileBlock.GlobalEnabled)
                DeepProfiler.End();
        }
    }
}
