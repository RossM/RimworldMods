using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class HediffDefExtension_ForceBehavior : DefModExtension
    {
        public ThinkTreeDef thinkTree;
        public MentalStateDef mentalState;
        public string iconPath;
    }

    public class Hediff_ForceBehavior : HediffWithComps
    {
        private MoteBubble mote;

        public HediffDefExtension_ForceBehavior DefExt => def.GetModExtension<HediffDefExtension_ForceBehavior>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mote, "mote");
        }

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);

            pawn.jobs.StopAll();

            if (DefExt.mentalState != null)
            {
                pawn.mindState.mentalStateHandler.TryStartMentalState(DefExt.mentalState, forced: true, forceWake: true,
                    causedByDamage: true);
            }

            if (DefExt.iconPath != null)
            {
                mote = MoteMaker.MakeThoughtBubble(pawn, DefExt.iconPath, maintain: true);
                Log.Message(string.Format("Mote: {0}", mote));
            }
        }

        public override void Tick()
        {
            using (new ProfileBlock())
            {
                base.Tick();

                mote?.Maintain();

                if (pawn.Downed)
                    pawn.health.RemoveHediff(this);
            }
        }

        public override void PostRemoved()
        {
            base.PostRemoved();

            pawn.jobs.StopAll();

            if (DefExt.mentalState != null)
                pawn.mindState.mentalStateHandler.CurState.RecoverFromState();

            mote?.Destroy();
        }
    }
}
