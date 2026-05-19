using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(MainTabWindow_Wildlife))]
    public static class Patch_MainTabWindow_Wildlife
    {
        [Feature(typeof(IncidentWorker_WildTribe))]
        [HarmonyPrefix]
        [UsedImplicitly]
        [HarmonyPatch("Pawns", MethodType.Getter)]
        public static bool Pawns_Prefix(MainTabWindow_Wildlife __instance, ref IEnumerable<Pawn> __result)
        {
            bool IsWildlife(Pawn p) => p.Spawned && (p.Faction == null || !p.Faction.def.humanlikeFaction) && p.AnimalOrWildMan() &&
                        !p.Position.Fogged(p.Map) && !p.IsPrisonerInPrisonCell();


            __result = Find.CurrentMap.mapPawns.AllPawns.Where(IsWildlife);
            return false;
        }
    }
}
