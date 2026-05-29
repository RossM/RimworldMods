using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace XylXenos.Patches
{
    [HarmonyPatch(typeof(PawnGroupKindWorker_Trader))]
    public static class Patch_PawnGroupKindWorker_Trader
    {
        [Feature(nameof(DefOf.XylTribeGentleNixie))]
        [HarmonyPrefix]
        [HarmonyPatch("GenerateCarriers")]
        public static bool GenerateCarriers_Prefix(
            PawnGroupMakerParms parms,
            PawnGroupMaker groupMaker,
            Pawn trader,
            List<Thing> wares,
            List<Pawn> outPawns)
        {
            if (parms.faction.def != DefOf.XylTribeGentleNixie)
                return true;

            // This has two differences from the normal version:
            // (1) Selkies are excluded from non-coastal, non-river tiles
            // (2) Fewer animals are generated if mastodons are selected as the carrier type

            List<Thing> waresItems = wares.Where(thing => thing is not Pawn).ToList();
            int itemIndex = 0;
            IEnumerable<PawnGenOption> carrierOptions = groupMaker.carriers;
            if (parms.tile.Valid)
            {
                Tile tile = Find.WorldGrid[parms.tile];
                if (!tile.IsCoastalOrRiverTile)
                    carrierOptions = carrierOptions.Where(genOption => genOption.kind != DefOf.XylSelkie);
                carrierOptions = carrierOptions.Where(genOption => tile.PrimaryBiome.IsPackAnimalAllowed(genOption.kind.race));
            }

            PawnKindDef kind = carrierOptions.RandomElementByWeight(genOption => genOption.selectionWeight).kind;
            int numAnimals = Mathf.CeilToInt(waresItems.Count / (kind.race.race.baseBodySize <= 4.0f ? 8f : 16f));

            List<Pawn> carrierPawns = [];
            for (int i = 0; i < numAnimals; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, parms.faction, PawnGenerationContext.NonPlayer,
                    parms.tile, forceGenerateNewPawn: false, allowDead: false, allowDowned: false, canGeneratePawnRelations: true,
                    mustBeCapableOfViolence: false, forceAddFreeWarmLayerIfNeeded: false, allowGay: true, allowPregnant: false,
                    allowFood: true, allowAddictions: true, fixedIdeo: parms.ideo, inhabitant: parms.inhabitants));
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
}
