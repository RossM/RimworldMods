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
        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(Pawn_Thinker.MainThinkTree), MethodType.Getter)]
        static bool MainThinkTree_Prefix(Pawn_Thinker __instance, ref ThinkTreeDef __result)
        {
            var pawn = __instance.pawn;
            foreach (var hediff in pawn.health.hediffSet.hediffs.OfType<Hediff_ForceBehavior>())
            {
                __result = hediff.DefExt.thinkTree;
                return false;
            }

            return true;
        }
    }
}
