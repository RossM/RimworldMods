using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylXenos
{
    public class HostilityOverrideManager(Map map) : MapComponent(map), INotificationListener
    {
        public const int violationDisableTicks = 2500;
        public const int updateFrequency = 60;

        [Unsaved] private static Map lastMap;
        [Unsaved] private static HostilityOverrideManager lastManager;

        public HashSet<(Faction, Faction)> activeOverrides = [];
        public Dictionary<Faction, int> lastHostileActionTick = new();

        public override void ExposeData()
        {
            Scribe_Ext.Look(ref activeOverrides, nameof(activeOverrides), LookMode.Reference);

            Scribe_Collections.Look(ref lastHostileActionTick, nameof(lastHostileActionTick), keyLookMode: LookMode.Deep,
                valueLookMode: LookMode.Value);
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

        public bool HostilityDisabled(Thing source, Thing target)
        {
            if (target is not Pawn targetPawn)
                return false;

            if (!HasAnyOverride(target.Faction, source.Faction))
                return false;

            return targetPawn.IsColonyAnimal ||
                   targetPawn.GeneSet?.disableHostilityFromFactions?.Any(factionDef => factionDef == source.Faction?.def) == true;
        }

        public bool HasAnyOverride(Faction from, Faction to)
        {
            if (activeOverrides == null || !activeOverrides.Contains((from, to)))
                return false;

            if (lastHostileActionTick == null || !lastHostileActionTick.TryGetValue(from, out int hostileActionTick))
                return true;

            return hostileActionTick + violationDisableTicks < Find.TickManager.TicksGame;
        }

        public void Notify_DamageTaken(Thing target, DamageInfo info)
        {
            if (target?.Map != map)
                return;

            Thing source = info.Instigator;

            if (source?.Faction == null || target?.Faction == null)
                return;

            if (activeOverrides.Contains((source.Faction, target.Faction)))
                lastHostileActionTick[source.Faction] = Find.TickManager.TicksGame;
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % updateFrequency != 0)
                return;

            activeOverrides.Clear();
            foreach (var pawn in map.mapPawns.AllPawns)
            {
                List<FactionDef> factions = pawn.GeneSet?.disableHostilityFromFactions;
                if (factions == null)
                    continue;

                foreach (var factionDef in factions)
                {
                    foreach (var targetFaction in Find.FactionManager.AllFactions.Where(faction => faction.def == factionDef))
                    {
                        activeOverrides.Add((pawn.Faction, targetFaction));
                    }
                }
            }
        }

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register<DamageInfo>(NotificationEvent.PreDamageTaken, null, Notify_DamageTaken);
        }
    }
}
