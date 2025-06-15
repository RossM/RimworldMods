using RimWorld;
using System.Collections.Generic;
using System.Security.Cryptography;
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
            JobGiver_GetWetness obj = (JobGiver_GetWetness)base.DeepCopy(resolve);
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

        private void GetSearchSet(Pawn pawn, List<Thing> outCandidates)
        {
            outCandidates.Clear();
            if (thingDefs == null)
            {
                return;
            }
            if (thingDefs.Count == 1)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(thingDefs[0]));
                return;
            }
            for (int i = 0; i < thingDefs.Count; i++)
            {
                outCandidates.AddRange(pawn.Map.listerThings.ThingsOfDef(thingDefs[i]));
            }
        }

        private bool CanInteractWith(Pawn pawn, Thing t)
        {
            if (!pawn.CanReserve(t, 1))
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

            Thing bestThing = FindBestShower(pawn);
            Log.Message($"Best shower: {bestThing}");
            if (bestThing != null)
                return JobMaker.MakeJob(showerJobDef, bestThing);

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
