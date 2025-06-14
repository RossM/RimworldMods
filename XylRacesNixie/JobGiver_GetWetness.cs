using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesNixie
{
    public class JobGiver_GetWetness : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Log.Message("Called JobGiver_GetWetness.TryGiveJob()");

            var need_wetness = pawn.needs?.TryGetNeed<Need_Wetness>();
            if (need_wetness == null)
                return null;
            if (need_wetness.CurLevel > 0.99f)
                return null;

            bool CellValidator(IntVec3 vec)
            {
                Map map = pawn.Map;
                if (PawnUtility.KnownDangerAt(vec, map, pawn))
                    return false;
                if (vec.Fogged(map))
                    return false;
                if (vec.IsForbidden(pawn))
                    return false;

                TerrainDef terrainDef = vec.GetTerrain(map);
                if (terrainDef.traversedThought?.defName != "SoakingWet")
                    return false;
                if (!vec.Standable(map))
                    return false;

                return true;
            }

            if (RCellFinder.TryFindRandomCellNearWith(pawn.Position, CellValidator, pawn.Map, out IntVec3 result, 5, 25))
            {
                // TODO: GoSwimming seems to be an Odyssey job
                JobDef swim = JobDefOf.GoSwimming;
                Job job = JobMaker.MakeJob(swim, result);
                if (job != null)
                {
                    job.locomotionUrgency = LocomotionUrgency.Walk;
                    return job;
                }
            }

            //bool RegionValidator(Region region)
            //{
            //    if (!region.Room.PsychologicallyOutdoors)
            //        return false;
            //    if (region.IsForbiddenEntirely(pawn))
            //        return false;
            //    if (!region.TryFindRandomCellInRegionUnforbidden(pawn, CellValidator, out IntVec3 root))
            //        return false;

            //    return true;
            //}

            //if (CellFinder.TryFindClosestRegionWith(pawn.GetRegion(RegionType.Set_Passable), TraverseParms.For(pawn),
            //        RegionValidator, 100, out Region result, RegionType.Set_Passable))
            //{
            //    if (result.TryFindRandomCellInRegionUnforbidden(pawn, CellValidator, out IntVec3 root))
            //    {
            //        JobDef swim = DefDatabase<JobDef>.GetNamed("Swim");
            //        Job job = JobMaker.MakeJob(swim, root);
            //        job.locomotionUrgency = LocomotionUrgency.Walk;
            //        return job;
            //    }
            //}

            return null;
        }

        public override float GetPriority(Pawn pawn)
        {
            Log.Message("Called JobGiver_GetWetness.GetPriority()");

            var need_wetness = pawn.needs?.TryGetNeed<Need_Wetness>();
            if (need_wetness == null)
                return 0.0f;

            if (pawn.timetable?.CurrentAssignment == TimeAssignmentDefOf.Anything && need_wetness.CurCategory != WetnessCategory.Wet)
                return ThinkNodePriority.MiscNeed;

            return ThinkNodePriority.AnythingJoy;
        }
    }
}
