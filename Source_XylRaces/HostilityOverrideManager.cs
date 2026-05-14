using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    public class HostilityOverrideManager(Map map) : MapComponent(map)
    {
        [Unsaved]
        private static Map lastMap;
        [Unsaved]
        private static HostilityOverrideManager lastManager;

        public const int violationDisableTicks = 2500;

        public HashSet<(Faction, Faction)> activeOverrides = new();
        public Dictionary<Faction, int> lastHostileActionTick = new();

        public override void ExposeData()
        {
            Look(ref activeOverrides, nameof(activeOverrides), LookMode.Reference);

            Scribe_Collections.Look(ref lastHostileActionTick, nameof(lastHostileActionTick), keyLookMode: LookMode.Deep, valueLookMode: LookMode.Value);
        }

        private static void Look<T1, T2>(ref HashSet<(T1, T2)> valueTuples, string label, LookMode lookMode)
        {
            List<T1> listFirst = new();
            List<T2> listSecond = new();
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                foreach (var pair in valueTuples)
                {
                    listFirst.Add(pair.Item1);
                    listSecond.Add(pair.Item2);
                }
            }
            
            Scribe_Collections.Look(ref listFirst, label + "_first", lookMode);
            Scribe_Collections.Look(ref listSecond, label + "_second", lookMode);

            if ((lookMode == LookMode.Reference && Scribe.mode == LoadSaveMode.ResolvingCrossRefs) ||
                (lookMode != LookMode.Reference && Scribe.mode == LoadSaveMode.LoadingVars))
            {
                valueTuples = [];
                if (listFirst != null && listSecond != null)
                {
                    foreach (var pair in listFirst.Zip(listSecond, (f, s) => (f, s)))
                        valueTuples.Add(pair);
                }
            }
        }

        public static HostilityOverrideManager GetManager(Map map)
        {
            if (map == null)
                return null;
            if (map == lastMap)
                return lastManager;

            lastMap = map;
            lastManager = map.GetComponent<HostilityOverrideManager>();
            return lastManager;
        }

        public bool HasAnyOverride(Faction from, Faction to)
        {
            if (!activeOverrides.Contains((from, to)))
                return false;

            if (!lastHostileActionTick.TryGetValue(from, out int hostileActionTick)) 
                return true;

            return hostileActionTick + violationDisableTicks < Find.TickManager.TicksGame;
        }

        public void Notify_PawnDamagedThing(Pawn pawn, Thing thing)
        {
            if (activeOverrides.Contains((pawn.Faction, thing.Faction)))
                lastHostileActionTick[pawn.Faction] = Find.TickManager.TicksGame;
        }

        public override void MapComponentTick()
        {
            activeOverrides.Clear();
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                foreach (var defExt in pawn.ActiveGeneDefExtensionsOfType<GeneDefExtension_HostilityOverride>())
                {
                    FactionDef factionDef = defExt.disableHostilityFromFaction;
                    foreach (var targetFaction in Find.FactionManager.AllFactions.Where(faction => faction.def == factionDef))
                    {
                        activeOverrides.Add((pawn.Faction, targetFaction));
                    }
                }
            }
        }
    }
}
