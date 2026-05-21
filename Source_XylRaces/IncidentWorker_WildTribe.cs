using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class IncidentDefExtension_WildTribe : DefModExtension
    {
        public class TraitChance
        {
            public TraitDef trait;
            public float chance = 1.0f;

            [UsedImplicitly]
            public void LoadDataFromXmlCustom(XmlNode xmlRoot)
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "trait", xmlRoot.Name);
                chance = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
            }
        }

        public IntRange pawnsCount = new(2, 4);
        public IntRange exitMapTicks = new(180000, 300000);

        public FactionDef faction;
        public List<TraitChance> forcedTraits;
    }

    [UsedFromXml]
    public class IncidentWorker_WildTribe : IncidentWorker
    {
        public IncidentDefExtension_WildTribe DefExt => def.GetModExtension<IncidentDefExtension_WildTribe>();

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return TryFindEntryCell((Map)parms.target, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            if (!TryFindEntryCell(map, out IntVec3 start))
                return false;

            Faction faction = GenerateFaction();

            Rot4 rot = Rot4.FromAngleFlat((map.Center - start).AngleFlat);
            List<Pawn> pawns = GeneratePawns(faction);

            int exitMapTicks = DefExt.exitMapTicks.RandomInRange;

            foreach (Pawn pawn in pawns)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(start, map, 10);
                GenSpawn.Spawn(pawn, loc, map, rot);
                pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + exitMapTicks;
            }

            string pawnsPlural = DefExt.faction.pawnsPlural ?? "XylWildPeople".TranslateSimple();
            TaggedString baseLetterText = def.letterText.Formatted(pawnsPlural).CapitalizeFirst();
            string text = string.Format(def.letterLabel, pawnsPlural.CapitalizeFirst());
            SendStandardLetter(text, baseLetterText, def.letterDef, parms, pawns[0]);
            return true;
        }

        private Faction GenerateFaction()
        {
            List<FactionRelation> factionRelations = Find.FactionManager.AllFactionsListForReading
                .Where(item => !item.def.PermanentlyHostileTo(DefExt.faction))
                .Select(item => new FactionRelation() { other = item, kind = FactionRelationKind.Neutral })
                .ToList();
            Faction faction = FactionGenerator.NewGeneratedFactionWithRelations(DefExt.faction, factionRelations, hidden: true);
            faction.temporary = true;
            Find.FactionManager.Add(faction);
            return faction;
        }

        private bool TryFindEntryCell(Map map, out IntVec3 start)
        {
            return RCellFinder.TryFindRandomPawnEntryCell(out start, map, CellFinder.EdgeRoadChance_Animal);
        }

        private List<Pawn> GeneratePawns(Faction faction)
        {
            int count = DefExt.pawnsCount.RandomInRange;
            List<Pawn> pawns = [];

            for (int i = 0; i < count; i++)
            {
                DevelopmentalStage stage = (Find.Storyteller.difficulty.ChildrenAllowed
                    ? (DevelopmentalStage.Child | DevelopmentalStage.Adult)
                    : DevelopmentalStage.Adult);
                List<TraitDef> traits = DefExt.forcedTraits.Where(t => Rand.Chance(t.chance)).Select(t => t.trait).ToList();
                Pawn pawn = PawnGenerator.GeneratePawn(new(
                    kind: PawnKindDefOf.WildMan,
                    faction: faction,
                    context: PawnGenerationContext.NonPlayer,
                    forcedTraits: traits,
                    developmentalStages: stage));
                pawns.Add(pawn);
            }

            return pawns;
        }
    }
}
