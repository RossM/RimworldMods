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

        public override ThinkNode DeepCopy(bool resolve = true)
        {
            var obj = (JobGiver_GetWetness)base.DeepCopy(resolve);
            obj.thingDefs = thingDefs;
            obj.showerJobDef = showerJobDef;
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
            if (x.GetTerrain(pawn.Map).toxicBuildupFactor != 0f) 
                return false;
            if (x.Fogged(pawn.Map)) 
                return false;
            if (!x.Standable(pawn.Map)) 
                return false;

            WeatherDef curWeatherLerped = pawn.Map.weatherManager.CurWeatherLerped;
            if (curWeatherLerped.rainRate > 0.25f && !x.Roofed(pawn.Map))
                return true;

            if (x.GetTerrain(pawn.Map).IsWater) 
                return true;

            return false;
        }


        private bool TryFindWaterTile(Pawn pawn, out IntVec3 result, int maxSearchRadius = int.MaxValue)
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
            // TODO: Hook up GoSwimming when Odyssey comes out

            IntVec3 foundTile;
            if (IsValidWaterTileFor(pawn, pawn.Position))
            {
                // Wander around a bit
                if (Rand.Chance(0.2f) && TryFindWaterTile(pawn, out foundTile, 10))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Goto, foundTile);
                    job.locomotionUrgency = LocomotionUrgency.Walk;
                    return job;
                }
                else
                {
                    Job job = JobMaker.MakeJob(JobDefOf.Wait);
                    job.expiryInterval = 300;
                    return job;
                }
            }

            Thing bestThing = FindBestShower(pawn);
            if (bestThing != null)
            {
                Job job = JobMaker.MakeJob(showerJobDef, bestThing);
                job.reportStringOverride = null;
                return job;
            }

            if (TryFindWaterTile(pawn, out foundTile))
            {
                return JobMaker.MakeJob(JobDefOf.Goto, foundTile);
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
