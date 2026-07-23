using RimWorld.Planet;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(PawnGroupKindWorker_Trader))]
public static class Patch_PawnGroupKindWorker_Trader
{
    [Feature(nameof(DefOf.XylTribeGentleNixie))]
    [Prefix]
    [Target("GenerateCarriers")]
    public static bool GenerateCarriers_Prefix(
        PawnGroupMakerParms parms,
        PawnGroupMaker groupMaker,
        Pawn trader,
        List<Thing> wares,
        List<Pawn> outPawns)
    {
        DebugAssert.NotNull(Find.WorldGrid);
        DebugAssert.True(groupMaker.carriers is { Count: > 0 });

        if (parms.faction?.def != DefOf.XylTribeGentleNixie)
            return true;

        // This has two differences from the normal version:
        // (1) Selkies are excluded from non-coastal, non-river tiles
        // (2) Fewer animals are generated if mastodons are selected as the carrier type

        List<Thing> waresItems = [.. wares.Where(thing => thing is not Pawn)];
        int itemIndex = 0;
        IEnumerable<PawnGenOption> carrierOptions = groupMaker.carriers;
        if (parms.tile.Valid)
        {
            DebugAssert.NotNull(Find.WorldGrid);

            Tile tile = Find.WorldGrid[parms.tile];
            DebugAssert.NotNull(tile);
            DebugAssert.NotNull(tile.PrimaryBiome);

            if (!tile.IsCoastalOrRiverTile)
                carrierOptions = carrierOptions.Where(option => option.kind != DefOf.XylSelkie);
            carrierOptions = carrierOptions.Where(option =>
            {
                DebugAssert.NotNull(option.kind);
                DebugAssert.NotNull(option.kind.race);

                return tile.PrimaryBiome.IsPackAnimalAllowed(option.kind.race);
            });
        }

        PawnKindDef? kind = carrierOptions.RandomElementByWeight(genOption => genOption.selectionWeight).kind;
        DebugAssert.NotNull(kind);
        DebugAssert.NotNull(kind.race);
        DebugAssert.NotNull(kind.race.race);

        int numAnimals = Mathf.CeilToInt(waresItems.Count / (kind.race.race.baseBodySize <= 4.0f ? 8f : 16f));

        List<Pawn> carrierPawns = [];
        for (int i = 0; i < numAnimals; i++)
        {
            Pawn? pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, parms.faction, PawnGenerationContext.NonPlayer,
                parms.tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true,
                mustBeCapableOfViolence: false, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false,
                allowFood: true, allowAddictions: true, fixedIdeo: parms.ideo, inhabitant: parms.inhabitants));
            if (pawn == null)
                continue;

            if (itemIndex < waresItems.Count)
            {
                pawn.inventory.innerContainer.TryAdd(waresItems[itemIndex]);
                itemIndex++;
            }

            carrierPawns.Add(pawn);
            outPawns.Add(pawn);
        }

        for (; itemIndex < waresItems.Count; itemIndex++)
        {
            carrierPawns.RandomElement().inventory.innerContainer.TryAdd(waresItems[itemIndex]);
        }

        return false;
    }
}
