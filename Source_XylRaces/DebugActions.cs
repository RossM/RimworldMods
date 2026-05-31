namespace XylXenos;

public static class DebugActions
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

        var xenotypes = DefDatabase<XenotypeDef>.AllDefs.ToList();

        foreach (var pawnKindDef in pawnKindDefs)
        {
            foreach (var xenotype in xenotypes)
            {
                PawnKindDef newPawnKindDef = Gen.MemberwiseClone(pawnKindDef);
                newPawnKindDef.useFactionXenotypes = false;
                newPawnKindDef.xenotypeSet = new XenotypeSet();
                newPawnKindDef.xenotypeSet.xenotypeChances = [new(xenotype, 1.0f)];
                newPawnKindDef.defName = $"{pawnKindDef.defName}_{xenotype.defName}";
                newPawnKindDef.label = $"{xenotype.label} {pawnKindDef.label}";
                newPawnKindDef.ignoreFactionApparelStuffRequirements = true;
                newPawnKindDef.combatPower = pawnKindDef.combatPower * xenotype.combatPowerFactor;
                pawnKindsForBattleRoyale.Add(newPawnKindDef);
            }
        }

        ArenaUtility.PerformBattleRoyale(pawnKindsForBattleRoyale);
    }
}
