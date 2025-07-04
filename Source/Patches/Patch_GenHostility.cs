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
            
            // This complicated check handles the case where a character with disabled hostility tames a wild insect.
            // Normally the other insects would then attack the player-controlled insect. This check prevents that
            // from happening. Note that there's no mechanism to allow the insects to become hostile if they are attacked;
            // I'm hoping that case is rare enough to not worry about.
            if (pawn.Faction == Faction.OfPlayerSilentFail &&
                pawn.kindDef.defaultFactionDef != null &&
                pawn.kindDef.defaultFactionDef == pawn2.Faction?.def && 
                ModsConfig.IsActive("Xylthixlm.Races.Trog"))
            {
                return true;
            }

            return false;
        }
    }
}
