using HarmonyLib;
using RimWorld;
using Verse;
using XylXenos.Genes;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(GenHostility))]
    public static class Patch_GenHostility
    {
        // Note: This patch is performance-sensitive
        [Feature(nameof(DefExt.disableHostilityFromFactions))]
        [HarmonyPrefix]
        [HarmonyPatch(nameof(GenHostility.HostileTo), [typeof(Thing), typeof(Thing)])]
        public static bool HostileTo_Prefix(Thing a, Thing b, out bool __result)
        {
            __result = false;

            // These are cases where we should respect the regular logic
            if (a.Destroyed || b.Destroyed || a == b)
                return true;
            if ((a.Faction == null && a.TryGetComp<CompCauseGameCondition>() != null) ||
                (b.Faction == null && b.TryGetComp<CompCauseGameCondition>() != null))
                return true;

            if (a is not Pawn pawn || b is not Pawn pawn2)
                return true;
            if (pawn.IsActivityDormant() || pawn2.IsActivityDormant())
                return true;
            if (pawn.kindDef.hostileToAll || pawn2.kindDef.hostileToAll)
                return true;

            return !DisableHostilityCheck(pawn, pawn2) && !DisableHostilityCheck(pawn2, pawn);
        }

        private static bool DisableHostilityCheck(Pawn pawn, Pawn pawn2)
        {
            var manager = HostilityOverrideManager.GetManager(pawn.Map);
            if (manager == null)
                return false;
            if (!manager.HasAnyOverride(pawn.Faction, pawn2.Faction))
                return false;

            return pawn.IsColonyAnimal || pawn.GeneSet()?.disableHostilityFromFactions?.Any(factionDef => factionDef == pawn2.Faction?.def) == true;
        }
    }
}
