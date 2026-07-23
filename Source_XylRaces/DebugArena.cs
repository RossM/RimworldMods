using System.IO;
using RimWorld.Planet;
using Verse.AI.Group;
using static Verse.ArenaUtility;

namespace XylXenos;

public static class DebugArena
{
    private const int maxFights = 5;

    private static readonly Dictionary<string, float> combatPowerTmp = new();

    private static readonly HashSet<PawnKindDef> badKinds =
    [
        PawnKindDefOf.Nociosphere,
        PawnKindDefOf.Revenant,
        PawnKindDefOf.FleshmassNucleus,
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Basic"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Carrier"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Clawer"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Barkskin"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Woodmaker"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Medicinemaker"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Berrymaker"),
        DefDatabase<PawnKindDef>.GetNamed("Dryad_Gaumaker"),
    ];

    [DebugAction("Autotests")]
    public static void BattleRoyaleByXenotype()
    {
        var pawnKindsForBattleRoyale = new List<PawnKindDef>();

        List<string> humanoidPawnKinds =
        [
            "Tribal_Archer",
            "Tribal_Berserker",
            "Mercenary_Slasher",
            "Mercenary_Gunner",
            "Empire_Fighter_StellicGuardMelee",
            "Empire_Fighter_StellicGuardRanged",
        ];

        List<string> otherPawnKinds =
        [
            // Select animals
            "Thrumbo",
            "Megasloth",
            "Elephant",
            "Bear_Grizzly",
            "Rhinoceros",
            "Warg",
            "Panther",
            "Muffalo",
            "Husky",
            "Cassowary",
            "Cobra",
            "GuineaPig",
            "Monkey",
            "Rat",
            "Hare",
            "Chicken",
            "Sparrow",

            // Insects
            "Megaspider",
            "Spelopede",
            "Megascarab",
            "Locust",
            "Larva",

            // Mechs
            "Mech_CentipedeBlaster",
            "Mech_CentipedeGunner",
            "Mech_CentipedeBurner",
            "Mech_Lancer",
            "Mech_Scyther",
            "Mech_Pikeman",
            "Mech_Termite",
        ];

        static bool ValidXenotype(XenotypeDef xenotype) => !xenotype.AllGenes.Any(def => def.disabledWorkTags.HasFlag(WorkTags.Violent));

        var xenotypes = DefDatabase<XenotypeDef>.AllDefs.Where(ValidXenotype).ToList();

        foreach (var pawnKind in humanoidPawnKinds)
        {
            var pawnKindDef = DefDatabase<PawnKindDef>.GetNamed(pawnKind);
            if (pawnKindDef == null)
                continue;

            foreach (var xenotype in xenotypes)
            {
                PawnKindDef newPawnKindDef = PawnKindWithXenotype(pawnKindDef, xenotype);
                pawnKindsForBattleRoyale.Add(newPawnKindDef);
            }
        }

        foreach (var pawnKind in otherPawnKinds)
        {
            var pawnKindDef = DefDatabase<PawnKindDef>.GetNamed(pawnKind);
            if (pawnKindDef == null)
                continue;

            pawnKindsForBattleRoyale.Add(pawnKindDef);
        }

        PerformBattleRoyale(pawnKindsForBattleRoyale);
    }

    [DebugAction("Autotests")]
    public static void BattleRoyaleByPawnKind()
    {
        List<PawnKindDef> pawnKindsForBattleRoyale = PawnKindsWithDefaultXenotype();

        PerformBattleRoyale(pawnKindsForBattleRoyale);
    }

    [DebugAction("Autotests")]
    public static List<DebugActionNode> BattleRoyaleTopN()
    {
        static bool ValidXenotype(XenotypeDef xenotype) => !xenotype.AllGenes.Any(def => def.disabledWorkTags.HasFlag(WorkTags.Violent));

        var xenotypes = DefDatabase<XenotypeDef>.AllDefs.Where(ValidXenotype).ToList();

        List<PawnKindDef> pawnKindsForBattleRoyale = [];
        foreach (var pawnKindDef in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            if (badKinds.Contains(pawnKindDef))
                continue;

            if (pawnKindDef.RaceProps.Humanlike)
            {
                foreach (var xenotypeDef in xenotypes)
                {
                    pawnKindsForBattleRoyale.Add(PawnKindWithXenotype(pawnKindDef, xenotypeDef));
                }
            }
            else
            {
                pawnKindsForBattleRoyale.Add(pawnKindDef);
            }
        }

        List<DebugActionNode> actions = [];
        for (int n = 25; n <= 500; n += 25)
        {
            var localN = n;
            actions.Add(new($"Top {n}")
            {
                action = () => PerformBattleRoyale(pawnKindsForBattleRoyale, scoreRankLimit: localN),
            });
        }

        return actions;
    }

    [DebugAction("Autotests")]
    public static List<DebugActionNode> BattleRoyaleSpecificPawnKind()
    {
        static bool ValidXenotype(XenotypeDef xenotype) => !xenotype.AllGenes.Any(def => def.disabledWorkTags.HasFlag(WorkTags.Violent));

        var xenotypes = DefDatabase<XenotypeDef>.AllDefs.Where(ValidXenotype).ToList();

        List<PawnKindDef> pawnKindsForBattleRoyale = PawnKindsWithDefaultXenotype();

        List<DebugActionNode> actions = [];
        foreach (var pawnKindDef in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            if (pawnKindDef.RaceProps.Humanlike)
            {
                DebugActionNode node = new(pawnKindDef.defName)
                {
                    category = DebugToolsSpawning.GetCategoryForPawnKind(pawnKindDef),
                };
                foreach (var xenotype in xenotypes)
                {
                    var localKindDef = PawnKindWithXenotype(pawnKindDef, xenotype);
                    node.AddChild(new(xenotype.defName)
                    {
                        action = () =>
                        {
                            if (!pawnKindsForBattleRoyale.Contains(localKindDef))
                                pawnKindsForBattleRoyale.Add(localKindDef);
                            PerformBattleRoyale(pawnKindsForBattleRoyale, forcedPawnKind: localKindDef);
                        },
                    });
                }

                actions.Add(node);
            }
            else
            {
                var localKindDef = pawnKindDef;
                actions.Add(new(pawnKindDef.defName)
                {
                    category = DebugToolsSpawning.GetCategoryForPawnKind(pawnKindDef),
                    action = () =>
                    {
                        if (!pawnKindsForBattleRoyale.Contains(localKindDef))
                            pawnKindsForBattleRoyale.Add(localKindDef);
                        PerformBattleRoyale(pawnKindsForBattleRoyale, forcedPawnKind: localKindDef);
                    },
                });
            }
        }

        return actions;
    }

    private static List<PawnKindDef> PawnKindsWithDefaultXenotype()
    {
        List<PawnKindDef> pawnKindsForBattleRoyale = [];

        foreach (var pawnKindDef in DefDatabase<PawnKindDef>.AllDefsListForReading)
        {
            if (badKinds.Contains(pawnKindDef))
                continue;

            if (pawnKindDef.RaceProps.Humanlike)
            {
                pawnKindsForBattleRoyale.Add(PawnKindWithXenotype(pawnKindDef, GetDefaultXenotype(pawnKindDef)));
            }
            else
            {
                pawnKindsForBattleRoyale.Add(pawnKindDef);
            }
        }

        return pawnKindsForBattleRoyale;
    }

    private static XenotypeDef? GetDefaultXenotype(PawnKindDef pawnKindDef)
    {
        if ((pawnKindDef.xenotypeSet ?? pawnKindDef.defaultFactionDef?.xenotypeSet) is { } xenotypeSet)
            return xenotypeSet switch
            {
                [{ chance: >= 1 } value] => value.xenotype,
                XenotypeSetWithDefault withDefault => withDefault.defaultXenotype,
                _ => XenotypeDefOf.Baseliner
            };
        return XenotypeDefOf.Baseliner;
    }

    private static PawnKindDef PawnKindWithXenotype(PawnKindDef pawnKindDef, XenotypeDef? xenotype)
    {
        if (xenotype == null)
            return pawnKindDef;

        PawnKindDef newPawnKindDef = pawnKindDef.MemberwiseClone();
        newPawnKindDef.useFactionXenotypes = false;
        newPawnKindDef.xenotypeSet = new XenotypeSet
        {
            xenotypeChances = [new(xenotype, 1.0f)],
        };
        newPawnKindDef.defName = $"{pawnKindDef.defName}_{xenotype.defName}";
        newPawnKindDef.label = $"{xenotype.label} {pawnKindDef.label}";
        newPawnKindDef.ignoreFactionApparelStuffRequirements = true;
        newPawnKindDef.combatPower = pawnKindDef.combatPower * xenotype.combatPowerFactor;
        return newPawnKindDef;
    }

    public static void PerformBattleRoyale(
        IEnumerable<PawnKindDef> kindsEnumerable,
        int scoreRankLimit = -1,
        PawnKindDef? forcedPawnKind = null)
    {
        if (!ValidateArenaCapability())
            return;

        DebugAssert.NotNull(Current.Game);

        List<PawnKindDef> kinds = [.. kindsEnumerable];
        int currentFights = 0;

        Dictionary<PawnKindDef, int> wins = new();
        Dictionary<PawnKindDef, int> total = new();

        foreach (var def in kinds)
        {
            wins[def] = 0;
            total[def] = 0;
        }

        string resultsPath = GenFilePaths.SaveDataFolderPath + Path.DirectorySeparatorChar + "CombatArena.csv";
        string ratingsPath = GenFilePaths.SaveDataFolderPath + Path.DirectorySeparatorChar + "elo_ratings.csv";

        try
        {
            using var streamReader = new StreamReader(resultsPath);
            while (streamReader.ReadLine() is string line)
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

        GameComponent_DebugTools? debugTools = Current.Game.GetComponent<GameComponent_DebugTools>();
        DebugAssert.NotNull(debugTools);

        debugTools.AddPerFrameCallback(delegate
        {
            if (currentFights >= maxFights)
                return false;

            ReadEstimatedCombatPower(ratingsPath);

            float PawnKindWeight(PawnKindDef def) => Mathf.Pow(0.98f, total[def]);
            static float CombatPower(PawnKindDef def) => combatPowerTmp.TryGetValue(def.defName, out float value) ? value : def.combatPower;

            List<PawnKindDef> filteredKinds = kinds;
            if (scoreRankLimit > 0)
                filteredKinds = [.. kinds.OrderByDescending(CombatPower).Take(scoreRankLimit)];

            PawnKindDef lhsDef = forcedPawnKind ?? filteredKinds.RandomElementByWeight(PawnKindWeight);
            // ReSharper disable once AccessToModifiedClosure
            PawnKindDef rhsDef = filteredKinds.Where(def => def != lhsDef).RandomElementByWeight(PawnKindWeight);

            if (forcedPawnKind != null && Rand.Chance(0.5f))
            {
                (lhsDef, rhsDef) = (rhsDef, lhsDef);
            }

            float lhsPower = CombatPower(lhsDef);
            float rhsPower = CombatPower(rhsDef);

            lhsPower = Mathf.Clamp(lhsPower, 20, 800);
            rhsPower = Mathf.Clamp(rhsPower, 20, 800);

            int totalCombatants = RandRangeExponential(2, 40);

            int lhsCount = GenMath.RoundRandom(totalCombatants * rhsPower / (lhsPower + rhsPower));
            int rhsCount = totalCombatants - lhsCount;

            if (lhsCount <= 0 || rhsCount <= 0)
                return false;

            List<PawnKindDef> lhs = [.. Enumerable.Repeat(lhsDef, lhsCount)];
            List<PawnKindDef> rhs = [.. Enumerable.Repeat(rhsDef, rhsCount)];

            if (BeginArenaFight(lhs, rhs, FightCompletedCallback))
                currentFights += 1;

            return false;

            void FightCompletedCallback(ArenaResult result)
            {
                // ReSharper disable once AccessToModifiedClosure
                currentFights -= 1;

                // Log to file
                using StreamWriter streamWriter = new StreamWriter(resultsPath, append: true);
                int score = result.winner switch
                {
                    ArenaResult.Winner.Other => 0,
                    ArenaResult.Winner.Lhs => 1,
                    ArenaResult.Winner.Rhs => -1,
                    _ => throw new ArgumentOutOfRangeException(),
                };
                streamWriter.WriteLine($"{lhsDef.defName},{lhs.Count},{rhsDef.defName},{rhs.Count},{score}");

                total[lhsDef] += 1;
                total[rhsDef] += 1;

                switch (result)
                {
                    case { winner: ArenaResult.Winner.Lhs }: wins[lhsDef] += 1; break;
                    case { winner: ArenaResult.Winner.Rhs }: wins[rhsDef] += 1; break;
                }
            }
        });
    }

    private static void ReadEstimatedCombatPower(string ratingsPath)
    {
        combatPowerTmp.Clear();

        // We read the ratings every time in case they have changed
        try
        {
            using var streamReader = new StreamReader(ratingsPath);
            while (streamReader.ReadLine() is string line)
            {
                var parts = line.Split(',');

                if (parts[0] == "unit_type")
                    continue;

                string unit_type = parts[0];
                int samples = int.Parse(parts[1]);
                float combat_power = float.Parse(parts[3]);

                if (samples >= Rand.Range(1, 30))
                    combatPowerTmp[unit_type] = combat_power;
            }
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static int RandRangeExponential(int min, int max)
    {
        return GenMath.RoundRandom(Mathf.Exp(Rand.Range(Mathf.Log(min), Mathf.Log(max))));
    }

    public static bool BeginArenaFight(List<PawnKindDef> lhs, List<PawnKindDef> rhs, Action<ArenaResult> callback)
    {
        DebugAssert.NotNull(Find.World?.tileTemperatures);
        DebugAssert.NotNull(Current.Game);

        var tile = TileFinder.RandomSettlementTileFor(Faction.OfPlayer, mustBeAutoChoosable: true,
            tile => lhs.Concat(rhs).Any(def => Find.World.tileTemperatures.SeasonAndOutdoorTemperatureAcceptableFor(tile, def.race!)));
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

            RimWorld.Planet.DebugArena? component = mapParent.GetComponent<RimWorld.Planet.DebugArena>();
            DebugAssert.NotNull(component);

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
        List<Pawn> list = [];
        foreach (PawnKindDef pawnKind in kinds)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: pawnKind,
                faction: faction,
                tile: map.Parent.Tile,
                forceGenerateNewPawn: true,
                mustBeCapableOfViolence: true);

            Pawn? pawn = PawnGenerator.GeneratePawn(request);

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
