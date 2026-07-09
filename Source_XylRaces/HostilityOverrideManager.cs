namespace XylXenos;

[UsedFromReflection]
public class HostilityOverrideManager(Map map) : MapComponent(map), IEventListener
{
    public const int violationDisableTicks = 2500;
    public const int updateFrequency = 60;

    [Unsaved] private static Map? lastMap;
    [Unsaved] private static HostilityOverrideManager? lastManager;

    public bool anyOverrides = false;
    public HashSet<(Faction, Faction)> activeOverrides = [];
    public Dictionary<Faction, int> lastHostileActionTick = new();

    public override void ExposeData()
    {
        Scribe_Values.Look(ref anyOverrides, nameof(anyOverrides));
        Scribe_Ext.Look(ref activeOverrides, nameof(activeOverrides), LookMode.Reference);
        Scribe_Collections.Look(ref lastHostileActionTick, nameof(lastHostileActionTick), keyLookMode: LookMode.Deep,
            valueLookMode: LookMode.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HostilityOverrideManager? GetManager(Map? map)
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
        if (!anyOverrides)
            return false;

        if (target is not Pawn targetPawn)
            return false;

        if (!HasAnyOverride(target.Faction, source.Faction))
            return false;

        return targetPawn.IsColonyAnimal ||
               targetPawn.GeneTracker_XylXenos?.disableHostilityFromFactions?.Contains(source.Faction!.def) is true;
    }

    private bool HasAnyOverride(Faction from, Faction to)
    {
        if (!activeOverrides.Contains((from, to)))
            return false;

        if (!lastHostileActionTick.TryGetValue(from, out int hostileActionTick))
            return true;

        return hostileActionTick + violationDisableTicks < Find.TickManager.TicksGame;
    }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % updateFrequency != 0)
            return;

        anyOverrides = false;
        activeOverrides.Clear();

        HashSet<Faction> mapFactions = [];
        foreach (var pawn in map.mapPawns.AllPawns)
        {
            if (pawn.Faction is Faction faction)
                mapFactions.Add(faction);
        }

        foreach (var pawn in map.mapPawns.AllPawns)
        {
            List<FactionDef>? factions = pawn.GeneTracker_XylXenos?.disableHostilityFromFactions;
            if (factions is not { Count: > 0 })
                continue;

            foreach (var factionDef in factions)
            {
                foreach (var targetFaction in mapFactions.Where(faction => faction.def == factionDef))
                {
                    anyOverrides = true;
                    activeOverrides.Add((pawn.Faction, targetFaction));
                }
            }
        }
    }

    public void Notify_DamageTaken(Thing? target, DamageInfo info)
    {
        if (target?.Map != map)
            return;

        Thing source = info.Instigator;

        if (source?.Faction == null || target.Faction == null)
            return;

        if (activeOverrides.Contains((source.Faction, target.Faction)))
            lastHostileActionTick[source.Faction] = Find.TickManager.TicksGame;
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register<DamageInfo>(EventDefOf.PreTakeDamage, null, Notify_DamageTaken);
    }

    public void PreUnregister(EventManager manager)
    {
    }
}
