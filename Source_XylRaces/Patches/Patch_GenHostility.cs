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
        public static bool? Gene_HostilityOverride_Enabled;

        [HarmonyPrefix, UsedImplicitly, HarmonyPatch(nameof(GenHostility.HostileTo), [typeof(Thing), typeof(Thing)])]
        public static bool HostileTo_Prefix(Thing a, Thing b, ref bool __result)
        {
            Gene_HostilityOverride_Enabled ??= Config.FeatureEnabled(Config.Feature.Gene_HostilityOverride);
            if (Gene_HostilityOverride_Enabled == false)
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
                if (pawn.HasActiveGeneOfType<HostilityOverride>(g => g.DisableHostility(pawn2)))
                    return true;

                // When a character with a hostility-disabling gene tames a wild insect, the insect would immediately
                // be attacked by its former allies. This prevents that.
                if (pawn.playerSettings?.Master?.HasActiveGeneOfType<HostilityOverride>(g => g.DisableHostility(pawn2)) == true)
                    return true;

                return false;
            }
        }
    }
}
