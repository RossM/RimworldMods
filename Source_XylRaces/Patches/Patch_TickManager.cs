using HarmonyLib;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(TickManager))]
    public static class Patch_TickManager
    {
        [Feature(nameof(ProfileBlock.InstrumentTickManager)), HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(TickManager.DoSingleTick))]
        public static void DoSingleTick_Prefix()
        {
            if (ProfileBlock.InstrumentTickManager)
                DeepProfiler.Start("DoSingleTick");
        }

        [Feature(nameof(ProfileBlock.InstrumentTickManager)), HarmonyPostfix, UsedImplicitly,
         HarmonyPatch(nameof(TickManager.DoSingleTick))]
        public static void DoSingleTick_Postfix()
        {
            if (ProfileBlock.InstrumentTickManager)
                DeepProfiler.End();
        }
    }
}
