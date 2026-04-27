using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(Pawn_Thinker))]
    public static class Patch_Pawn_Thinker
    {
        [Feature(nameof(Hediff_ForceBehavior)), HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_Thinker.MainThinkTree), MethodType.Getter)]
        public static bool MainThinkTree_Prefix(Pawn_Thinker __instance, ref ThinkTreeDef __result)
        {
            using (new ProfileBlock())
            {
                foreach (var hediff in __instance.pawn.HediffsOfType<Hediff_ForceBehavior>())
                {
                    __result = hediff.DefExt.thinkTree;
                    return false;
                }

                return true;
            }
        }
    }
}
