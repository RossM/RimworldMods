using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class JobGiver_GetWetness : ThinkNode_JobGiver
    {
        public List<ThingDef> thingDefs;
        public JobDef showerJobDef;
        public JobDef soakJobDef;

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var obj = (JobGiver_GetWetness)base.DeepCopy(resolve);
            obj.thingDefs = thingDefs;
            obj.showerJobDef = showerJobDef;
            obj.soakJobDef = soakJobDef;
            return obj;
        }

        private Thing FindBestShower(Pawn pawn)
        {
            var candidates = new List<Thing>();
            GetSearchSet(pawn, candidates);
            if (candidates.Count == 0)
                return null;

            return GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, candidates,
                PathEndMode.InteractionCell, TraverseParms.For(pawn),
                validator: t => CanInteractWith(pawn, t));
        }

        public static bool IsValidWaterTileFor(Pawn pawn, IntVec3 x)
        {
            if (PawnUtility.KnownDangerAt(x, pawn.Map, pawn)) 
                return false;
            TerrainDef terrain = x.GetTerrain(pawn.Map);
            if (terrain.toxicBuildupFactor != 0f) 
                return false;
            if (x.Fogged(pawn.Map)) 
                return false;
            if (!x.Standable(pawn.Map)) 
                return false;

            // Bathing in marsh is icky, only do it if really necessary.
            if (terrain.HasTag("WaterMarshy") && pawn.needs?.TryGetNeed<Need_Wetness>() is { CurCategory: >= WetnessCategory.Neutral })
                return false;

            return Need_Wetness.GetWetness(x, pawn.Map) >= 0.5f;
        }


        public static bool TryFindWaterTile(Pawn pawn, out IntVec3 result, int maxSearchRadius = int.MaxValue)
        {
            return RCellFinder.TryFindRandomCellNearWith(pawn.Position, x => IsValidWaterTileFor(pawn, x), pawn.Map, out result, maxSearchRadius: maxSearchRadius);

        }

        private void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (thingDefs == null)
            {
                return;
            }
            foreach (ThingDef def in thingDefs)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(def));
            }
        }

        private static bool CanInteractWith(Pawn pawn, Thing t)
        {
            if (!pawn.CanReserve(t))
            {
                return false;
            }
            if (t.IsForbidden(pawn))
            {
                return false;
            }
            if (t.Fogged())
            {
                return false;
            }
            if (!t.IsSociallyProper(pawn))
            {
                return false;
            }
            if (!t.IsPoliticallyProper(pawn))
            {
                return false;
            }
            return true;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            if (IsValidWaterTileFor(pawn, pawn.Position))
            {
                return JobMaker.MakeJob(soakJobDef, pawn.Position);
            }

            Thing bestThing = FindBestShower(pawn);
            if (bestThing != null)
            {
                return JobMaker.MakeJob(showerJobDef, bestThing);
            }

            if (TryFindWaterTile(pawn, out IntVec3 foundTile))
            {
                return JobMaker.MakeJob(soakJobDef, foundTile);
            }

            return null;
        }

        public override float GetPriority(Pawn pawn)
        {
            var need_wetness = pawn.needs?.TryGetNeed<Need_Wetness>();
            if (need_wetness == null || need_wetness.CurCategory == WetnessCategory.Wet)
                return 0.0f;

            return pawn.timetable?.CurrentAssignment == TimeAssignmentDefOf.Anything ? ThinkNodePriority.MiscNeed : ThinkNodePriority.AnythingJoy;
        }
    }
}
