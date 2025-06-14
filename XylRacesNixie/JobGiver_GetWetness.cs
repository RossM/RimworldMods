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

            // TODO: Hook up JoyGiver_GoSwimming when Odyssey comes out

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
