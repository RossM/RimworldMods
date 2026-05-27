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
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GenHostility.HostileTo), [typeof(Thing), typeof(Thing)])]
        public static void HostileTo_Postfix(Thing a, Thing b, ref bool __result)
        {
            if (!__result)
                return;

            // These are cases where we should respect the regular logic
            if (a.Destroyed || b.Destroyed || a == b)
                return;
            if ((a.Faction == null && a.TryGetComp<CompCauseGameCondition>() != null) ||
                (b.Faction == null && b.TryGetComp<CompCauseGameCondition>() != null))
                return;

            if ((a as Pawn)?.kindDef.hostileToAll == true || (b as Pawn)?.kindDef.hostileToAll == true)
                return;

            __result = !DisableHostilityCheck(a, b) && !DisableHostilityCheck(b, a);
        }

        private static bool DisableHostilityCheck(Thing a, Thing b)
        {
            if (a is not Pawn pawn)
                return false;

            var manager = HostilityOverrideManager.GetManager(a.Map);
            if (manager == null)
                return false;
            if (!manager.HasAnyOverride(a.Faction, b.Faction))
                return false;

            return pawn.IsColonyAnimal || pawn.GeneSet()?.disableHostilityFromFactions?.Any(factionDef => factionDef == b.Faction?.def) == true;
        }
    }
}
