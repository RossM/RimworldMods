using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace XylRacesCore
{
    public class CompProperties_AbilityWaterskipWetness : CompProperties_AbilityEffect
    {
        public float satisfaction = 0.5f;

        public CompProperties_AbilityWaterskipWetness()
        {
            compClass = typeof(CompAbilityEffect_WaterskipWetness);
        }
    }

    public class CompAbilityEffect_WaterskipWetness : CompAbilityEffect
    {
        public new CompProperties_AbilityWaterskipWetness Props => ((CompProperties_AbilityWaterskipWetness)props);

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.Map;
            foreach (IntVec3 item in AffectedCells(target, map))
            {
                if (!item.InBounds(map))
                {
                    continue;
                }
                List<Thing> thingList = item.GetThingList(map);
                for (int num = thingList.Count - 1; num >= 0; num--)
                {
                    if (thingList[num] is Pawn pawn)
                    {
                        var wetnessNeed = pawn.needs?.AllNeeds.OfType<Need_Wetness>().FirstOrDefault();
                        if (wetnessNeed != null)
                            wetnessNeed.CurLevel += Props.satisfaction;
                    }
                }
            }
        }

        private IEnumerable<IntVec3> AffectedCells(LocalTargetInfo target, Map map)
        {
            if (!target.Cell.InBounds(map) || target.Cell.Filled(parent.pawn.Map))
            {
                yield break;
            }
            foreach (IntVec3 item in GenRadial.RadialCellsAround(target.Cell, parent.def.EffectRadius, useCenter: true))
            {
                if (item.InBounds(map) && GenSight.LineOfSightToEdges(target.Cell, item, map, skipFirstCell: true))
                {
                    yield return item;
                }
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Cell.Filled(parent.pawn.Map))
            {
                if (throwMessages)
                {
                    Messages.Message("AbilityOccupiedCells".Translate(parent.def.LabelCap), target.ToTargetInfo(parent.pawn.Map), MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return true;
        }
    }
}
