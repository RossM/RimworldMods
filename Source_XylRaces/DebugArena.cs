using System.IO;
using RimWorld.Planet;
using Verse.AI.Group;
using static Verse.ArenaUtility;

namespace XylXenos;

public static class DebugArena
{
    [DebugAction("Autotests")]
    public static void BattleRoyaleByXenotype()
    {
        var pawnKindsForBattleRoyale = new List<PawnKindDef>();

        //var pawnKindDefs = DefDatabase<PawnKindDef>.AllDefs.Where((PawnKindDef k) => k.RaceProps.Humanlike).ToList();

        var pawnKindDefs = new List<PawnKindDef>
        {
            DefDatabase<PawnKindDef>.GetNamed("Tribal_Penitent"),
            DefDatabase<PawnKindDef>.GetNamed("Tribal_Archer"),
            DefDatabase<PawnKindDef>.GetNamed("Tribal_Berserker"),
            DefDatabase<PawnKindDef>.GetNamed("Scavenger"),
            DefDatabase<PawnKindDef>.GetNamed("Villager"),
            DefDatabase<PawnKindDef>.GetNamed("Town_Guard"),
            DefDatabase<PawnKindDef>.GetNamed("Grenadier_Destructive"),
            DefDatabase<PawnKindDef>.GetNamed("Mercenary_Gunner"),
            DefDatabase<PawnKindDef>.GetNamed("Mercenary_Sniper"),
            DefDatabase<PawnKindDef>.GetNamed("Mercenary_Slasher"),
        };

        if (ModLister.RoyaltyInstalled)
        {
            pawnKindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Empire_Fighter_Janissary"));
            pawnKindDefs.Add(DefDatabase<PawnKindDef>.GetNamed("Empire_Fighter_Cataphract"));
        }

        Dictionary<string, string> xenotypeSuffixes = new();

        var xenotypes = DefDatabase<XenotypeDef>.AllDefs.ToList();

        foreach (var pawnKindDef in pawnKindDefs)
        {
            foreach (var xenotype in xenotypes)
            {
                if (xenotype.AllGenes.Any(def => (def.disabledWorkTags & pawnKindDef.requiredWorkTags) != 0))
                    continue;

                if (!xenotypeSuffixes.TryGetValue(xenotype.defName, out string xenotypeSuffix))
                    xenotypeSuffix = "";

                PawnKindDef newPawnKindDef = Gen.MemberwiseClone(pawnKindDef);
                newPawnKindDef.useFactionXenotypes = false;
                newPawnKindDef.xenotypeSet = new XenotypeSet();
                newPawnKindDef.xenotypeSet.xenotypeChances = [new(xenotype, 1.0f)];
                newPawnKindDef.defName = $"{pawnKindDef.defName}_{xenotype.defName}{xenotypeSuffix}";
                newPawnKindDef.label = $"{xenotype.label} {pawnKindDef.label}";
                newPawnKindDef.ignoreFactionApparelStuffRequirements = true;
                newPawnKindDef.combatPower = pawnKindDef.combatPower * xenotype.combatPowerFactor;
                pawnKindsForBattleRoyale.Add(newPawnKindDef);
            }
        }

        PerformBattleRoyale(pawnKindsForBattleRoyale);
    }


    public static void PerformBattleRoyale(IEnumerable<PawnKindDef> kindsEnumerable)
    {
        if (!ValidateArenaCapability())
            return;
        List<PawnKindDef> kinds = kindsEnumerable.ToList();
        int currentFights = 0;

        Dictionary<PawnKindDef, int> wins = new();
        Dictionary<PawnKindDef, int> total = new();

        foreach (var def in kinds)
        {
            wins[def] = 0;
            total[def] = 0;
        }

        string path = GenFilePaths.SaveDataFolderPath + Path.DirectorySeparatorChar + "CombatArena.csv";
        try
        {
            using var streamReader = new StreamReader(path);
            while (streamReader.ReadLine() is { } line)
            {
                var parts = line.Split(',');
                var lhsDef = kinds.FirstOrDefault(def => def.defName == parts[0]);
                var rhsDef = kinds.FirstOrDefault(def => def.defName == parts[2]);
                var score = int.Parse(parts[4]);

                if (lhsDef != null)
                    total[lhsDef] += 1;
                if (rhsDef != null)
                    total[rhsDef] += 1;

                switch (score)
                {
                    case > 0 when lhsDef != null: wins[lhsDef] += 1; break;
                    case < 0 when rhsDef != null: wins[rhsDef] += 1; break;
                }
            }
        }
        catch (Exception)
        {
            // ignored
        }

        StringBuilder sb = new StringBuilder();
        foreach (var def in kinds)
            sb.AppendLine($"{def.defName}: {def.combatPower} combat power, {wins[def]} wins / {total[def]} total");
        Debug.Log(sb.ToString());

        Current.Game.GetComponent<GameComponent_DebugTools>().AddPerFrameCallback(delegate
        {
            if (currentFights >= 15)
                return false;

            int highestTotal = total.Values.Max();

            PawnKindDef lhsDef = kinds.RandomElementByWeight(def => 5 + (highestTotal - total[def]));
            PawnKindDef rhsDef = kinds.Where(def => def != lhsDef).RandomElementByWeight(def => 5 + (highestTotal - total[def]));

            // This is a quick-and-dirty heuristic that completely ignores the number of pawns on each side
            float lhsPower = (float)(wins[lhsDef] + 1) / (total[lhsDef] + 2);
            float rhsPower = (float)(wins[rhsDef] + 1) / (total[rhsDef] + 2);

            int totalCombatants = RandRangeExponential(2, 40);

            int lhsCount = GenMath.RoundRandom(totalCombatants * rhsPower / (lhsPower + rhsPower));
            int rhsCount = totalCombatants - lhsCount;

            if (lhsCount <= 0 || rhsCount <= 0)
                return false;

            List<PawnKindDef> lhs = Enumerable.Repeat(lhsDef, lhsCount).ToList();
            List<PawnKindDef> rhs = Enumerable.Repeat(rhsDef, rhsCount).ToList();

            if (BeginArenaFight(lhs, rhs, FightCompletedCallback))
                currentFights += 1;

            return false;

            void FightCompletedCallback(ArenaResult result)
            {
                currentFights -= 1;

                // Log to file
                using StreamWriter streamWriter = new StreamWriter(path, append: true);
                int score = result.winner switch
                {
                    ArenaResult.Winner.Other => 0,
                    ArenaResult.Winner.Lhs => 1,
                    ArenaResult.Winner.Rhs => -1,
                    _ => throw new ArgumentOutOfRangeException()
                };
                streamWriter.WriteLine($"{lhsDef.defName},{lhs.Count},{rhsDef.defName},{rhs.Count},{score}");

                total[lhsDef] += 1;
                total[rhsDef] += 1;

                switch (result.winner)
                {
                    case ArenaResult.Winner.Lhs: wins[lhsDef] += 1; break;
                    case ArenaResult.Winner.Rhs: wins[rhsDef] += 1; break;
                }
            }
        });
    }

    private static int RandRangeExponential(float min, float max)
    {
        return GenMath.RoundRandom(Mathf.Exp(Rand.Range(Mathf.Log(min), Mathf.Log(max))));
    }

    public static bool BeginArenaFight(List<PawnKindDef> lhs, List<PawnKindDef> rhs, Action<ArenaResult> callback)
    {
        var tile = TileFinder.RandomSettlementTileFor(Faction.OfPlayer, mustBeAutoChoosable: true,
            tile => lhs.Concat(rhs).Any(def => Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(tile, def.race)));
        Map map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(50, 1, 50), WorldObjectDefOf.Debug_Arena);

        try
        {
            MapParent mapParent = map.Parent;
            mapParent.SetFaction(Faction.OfPlayer);

            MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out var first, out var second);
            List<Pawn> lhs2 = SpawnPawnSet(map, lhs, first, Faction.OfAncients);
            List<Pawn> rhs2 = SpawnPawnSet(map, rhs, second, Faction.OfAncientsHostile);

            // Check that both sides actually spawned
            if (lhs2.Count == 0 || rhs2.Count == 0)
            {
                foreach (var pawn in lhs2.Concat(rhs2))
                {
                    if (!pawn.Destroyed)
                        pawn.Destroy();
                }

                mapParent.Destroy();
                return false;
            }

            RimWorld.Planet.DebugArena component = mapParent.GetComponent<RimWorld.Planet.DebugArena>();
            component.lhs = lhs2;
            component.rhs = rhs2;
            component.callback = callback;

            return true;
        }
        catch (Exception)
        {
            if (map is { Disposed: false })
                Current.Game.DeinitAndRemoveMap(map, false);
            throw;
        }
    }

    public static List<Pawn> SpawnPawnSet(Map map, List<PawnKindDef> kinds, IntVec3 spot, Faction faction)
    {
        List<Pawn> list = new List<Pawn>();
        for (int i = 0; i < kinds.Count; i++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(kinds[i], faction);

            // Check if pawn is null
            if (pawn == null)
                continue;

            IntVec3 loc = CellFinder.RandomClosewalkCellNear(spot, map, 12);
            if (GenSpawn.Spawn(pawn, loc, map, Rot4.Random) != null)
                list.Add(pawn);
            else
                pawn.Destroy();
        }

        LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(map.Center), map, list);
        return list;
    }
}
