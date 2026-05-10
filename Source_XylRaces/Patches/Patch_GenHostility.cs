using System;
using System.Linq;
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
        public static Lazy<bool> enabled = new(Config.GeneWithModExtensionExists<GeneDefExtension_HostilityOverride>);
        public static bool Enabled => enabled.Value;

        // Note: This patch is performance-sensitive
        [Feature(nameof(GeneDefExtension_HostilityOverride)), HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(GenHostility.HostileTo), [typeof(Thing), typeof(Thing)])]
        public static bool HostileTo_Prefix(Thing a, Thing b, ref bool __result)
        {
            if (!Enabled)
                return true;

            using (new ProfileBlock())
            {
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

                if (DisableHostilityCheck(pawn, pawn2) || DisableHostilityCheck(pawn2, pawn))
                {
                    __result = false;
                    return false;
                }

                // Continue to regular logic
                return true;
            }
        }

        private static bool DisableHostilityCheck(Pawn pawn, Pawn pawn2)
        {
            using (new ProfileBlock())
            {
                var manager = HostilityOverrideManager.GetManager(pawn.Map);
                if (manager == null)
                    return false;
                if (!manager.HasAnyOverride(pawn.Faction, pawn2.Faction))
                    return false;

                return pawn.IsColonyAnimal || pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_HostilityOverride>()
                    .Any(defExt => defExt.disableHostilityFromFaction == pawn2.Faction?.def);
            }
        }
    }
}
