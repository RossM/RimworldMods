using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore.Patches
{
    [HarmonyPatch(typeof(GenHostility))]
    public static class Patch_GenHostility
    {
        private static readonly MethodInfo methodIsActivityDormant =
            AccessTools.Method(typeof(GenHostility), "IsActivityDormant");

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(GenHostility.HostileTo), [typeof(Thing), typeof(Thing)])]
        public static bool HostileTo_Prefix(Thing a, Thing b, ref bool __result)
        {
            // These are cases where we should respect the regular logic
            if (a.Destroyed || b.Destroyed || a == b)
                return true;
            if ((a.Faction == null && a.TryGetComp<CompCauseGameCondition>() != null) || (b.Faction == null && b.TryGetComp<CompCauseGameCondition>() != null))
                return true;

            var pawn = a as Pawn;
            var pawn2 = b as Pawn;

            if (pawn == null || pawn2 == null)
                return true;
            if ((bool)methodIsActivityDormant.Invoke(null, [pawn]) || (bool)methodIsActivityDormant.Invoke(null, [pawn2]))
                return true;
            if (pawn.kindDef.hostileToAll || pawn2.kindDef.hostileToAll)
                return true;

            if (DisableHostilityCheck(pawn, pawn2) || DisableHostilityCheck(pawn2, pawn))
            {
                __result = false;
                return false;
            }

            // Continue to regular logic
            return true;
        }

        private static bool DisableHostilityCheck(Pawn pawn, Pawn pawn2)
        {
            if (pawn.HasGeneOfType<Gene_HostilityOverride>(g => g.DisableHostility(pawn2)))
                return true;

            // When a character with a hostility-disabling gene tames a wild insect, the insect would immediately
            // be attacked by its former allies. This prevents that.
            if (pawn.playerSettings?.Master?.HasGeneOfType<Gene_HostilityOverride>(g => g.DisableHostility(pawn2)) == true)
                return true;

            return false;
        }
    }
}
