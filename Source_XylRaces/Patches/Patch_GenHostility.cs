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

            if (a is not Pawn pawn || b is not Pawn pawn2)
                return;
            if (pawn.IsActivityDormant() || pawn2.IsActivityDormant())
                return;
            if (pawn.kindDef.hostileToAll || pawn2.kindDef.hostileToAll)
                return;

            __result = !DisableHostilityCheck(pawn, pawn2) && !DisableHostilityCheck(pawn2, pawn);
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
