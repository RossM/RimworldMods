using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class CompAbilityEffect_ToxicBurst : CompAbilityEffect_ReleaseGas
    {
        private new CompProperties_AbilityToxicBurst Props => (CompProperties_AbilityToxicBurst)props;

        private Pawn Pawn => parent.pawn;

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            int num = GenRadial.NumCellsInRadius(Props.AIUseRadius);

            float num2 = 0f;
            for (int i = 0; i < num; i++)
            {
                IntVec3 c = Pawn.Position + GenRadial.RadialPattern[i];
                if (!c.InBounds(Pawn.Map))
                {
                    continue;
                }
                List<Thing> thingList = c.GetThingList(Pawn.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    if (thingList[j] is Pawn pawn && pawn != Pawn && pawn.HostileTo(Pawn) && GasUtility.IsAffectedByExposure(pawn) && !pawn.IsPsychologicallyInvisible())
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
