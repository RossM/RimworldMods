using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Wildlife))]
    public static class Patch_MainTabWindow_Wildlife
    {
        [Feature(typeof(IncidentWorker_WildTribe))]
        [HarmonyPrefix]
        [HarmonyPatch("Pawns", MethodType.Getter)]
        public static bool Pawns_Prefix(MainTabWindow_Wildlife __instance, ref IEnumerable<Pawn> __result)
        {
            bool IsWildlife(Pawn pawn) =>
                pawn.Spawned &&
                (pawn.Faction == null || !pawn.Faction.def.humanlikeFaction) &&
                pawn.AnimalOrWildMan() &&
                !pawn.Position.Fogged(pawn.Map) &&
                !pawn.IsPrisonerInPrisonCell();

            __result = Find.CurrentMap.mapPawns.AllPawns.Where(IsWildlife);
            return false;
        }
    }
}
