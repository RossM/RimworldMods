using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using XylXenos.Genes;

namespace XylXenos
{
    public class MentalStateDefExtension_SeeingRed : DefModExtension
    {
        public string iconPath;
    }

    [UsedImplicitly]
    public class MentalState_SeeingRed : MentalState
    {
        public MentalStateDefExtension_SeeingRed DefExt => def.GetModExtension<MentalStateDefExtension_SeeingRed>();
        private MoteBubble mote;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref mote, nameof(mote));
        }

        public override bool ForceHostileTo(Thing t)
        {
            return pawn.HasActiveGeneOfType<SeeingRed>(g => g.ForceHostility(t));
        }

        public override RandomSocialMode SocialModeMax()
        {
            return RandomSocialMode.Off;
        }

        public override void PostStart(string reason)
        {
            base.PostStart(reason);

            if (!DefExt.iconPath.NullOrEmpty())
                mote = MoteMaker.MakeThoughtBubble(pawn, DefExt.iconPath, maintain: true);
        }

        public override void MentalStateTick(int delta)
        {
            base.MentalStateTick(delta);

            mote?.Maintain();
        }

        public override void PostEnd()
        {
            base.PostEnd();

            mote?.Destroy();
        }
    }
}
