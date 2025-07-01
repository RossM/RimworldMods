using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using static System.Collections.Specialized.BitVector32;

namespace XylRacesCore
{
    public class IncidentDefExtension_WildTribe : DefModExtension
    {
        public class TraitChance
        {
            public TraitDef trait;
            public float chance = 1.0f;
        }

        public IntRange pawnsCount = new(2, 4);
        public IntRange exitMapTicks = new(180000, 300000);

        public XenotypeDef xenotype;
        public List<TraitChance> forcedTraits;
        public List<MemeDef> forbiddenMemes;
        public List<MemeDef> preferredMemes;
    }

    public class IncidentWorker_WildTribe : IncidentWorker
    {
        public IncidentDefExtension_WildTribe DefExt => def.GetModExtension<IncidentDefExtension_WildTribe>();

        float IdeoWeight(Ideo ideo)
        {
            if (DefExt.forbiddenMemes != null && ideo.memes.Intersect(DefExt.forbiddenMemes).Any())
                return 0.0f;
            if (DefExt.preferredMemes != null && ideo.memes.Intersect(DefExt.preferredMemes).Any())
                return 10.0f;
            return 1.0f;
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return TryFindEntryCell((Map)parms.target, out _);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            var map = (Map)parms.target;
            if (!TryFindEntryCell(map, out IntVec3 start))
                return false;

            if (!Find.IdeoManager.IdeosListForReading.TryRandomElementByWeight(IdeoWeight, out Ideo ideo))
                ideo = null;

            //Log.Message(string.Format("Ideo: {0}", ideo?.name ?? "(none)"));
            //Log.Message("All ideos:");
            //foreach (var i in Find.IdeoManager.IdeosListForReading)
            //    Log.Message(string.Format("  {0} = {1}", i.name, IdeoWeight(i)));

            Rot4 rot = Rot4.FromAngleFlat((map.Center - start).AngleFlat);
            List<Pawn> pawns = GeneratePawns(ideo);

            int exitMapTicks = DefExt.exitMapTicks.RandomInRange;

            foreach (Pawn pawn in pawns)
            {
                IntVec3 loc = CellFinder.RandomClosewalkCellNear(start, map, 10);
                GenSpawn.Spawn(pawn, loc, map, rot);
                pawn.mindState.exitMapAfterTick = Find.TickManager.TicksGame + exitMapTicks;
            }

            string xenotypeDefLabel = DefExt.xenotype?.label ?? "XylWildPeople".TranslateSimple();
            TaggedString baseLetterText = def.letterText.Formatted(xenotypeDefLabel).CapitalizeFirst();
            string text = string.Format(def.letterLabel, xenotypeDefLabel.CapitalizeFirst());
            SendStandardLetter(text, baseLetterText, def.letterDef, parms, pawns[0]);
            return true;
        }

        bool TryFindEntryCell(Map map, out IntVec3 start)
        {
            return RCellFinder.TryFindRandomPawnEntryCell(out start, map, CellFinder.EdgeRoadChance_Animal);
        }

        List<Pawn> GeneratePawns(Ideo ideo)
        {
            int count = DefExt.pawnsCount.RandomInRange;
            List<Pawn> pawns = new();

            for (int i = 0; i < count; i++)
            {
                DevelopmentalStage stage = (Find.Storyteller.difficulty.ChildrenAllowed ? (DevelopmentalStage.Child | DevelopmentalStage.Adult) : DevelopmentalStage.Adult);
                PawnKindDef wildMan = PawnKindDefOf.WildMan;
                List<TraitDef> traits = DefExt.forcedTraits.Where(t => Rand.Chance(t.chance)).Select(t => t.trait).ToList();
                Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: wildMan, context: PawnGenerationContext.NonPlayer, forcedTraits: traits, forcedXenotype: DefExt.xenotype, fixedIdeo: ideo, developmentalStages: stage));
                pawns.Add(pawn);
            }

            return pawns;
        }
    }
}
