using RimWorld;
using Verse;

namespace XylXenos
{
    public class CompProperties_AbilityToxicBurst : CompProperties_AbilityReleaseGas
    {
        public float AIUseRadius;

        public CompProperties_AbilityToxicBurst()
        {
            compClass = typeof(CompAbilityEffect_ToxicBurst);
        }
    }

    public class CompAbilityEffect_ToxicBurst : CompAbilityEffect_ReleaseGas
    {
        private new CompProperties_AbilityToxicBurst Props => (CompProperties_AbilityToxicBurst)props;

        private Pawn Pawn => parent.pawn;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            int cellsInRadius = GenRadial.NumCellsInRadius(Props.AIUseRadius);

            for (int i = 0; i < cellsInRadius; i++)
            {
                IntVec3 c = Pawn.Position + GenRadial.RadialPattern[i];
                if (!c.InBounds(Pawn.Map))
                {
                    continue;
                }

                foreach (Thing thing in c.GetThingList(Pawn.Map))
                {
                    if (thing is Pawn pawn && pawn != Pawn && pawn.HostileTo(Pawn) && GasUtility.IsAffectedByExposure(pawn)
                        && !pawn.IsPsychologicallyInvisible())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
