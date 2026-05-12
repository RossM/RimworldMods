using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class CompProperties_AbilityRockToss : CompProperties_EffectWithDest
    {
        public float minRange;

        public CompProperties_AbilityRockToss()
        {
            compClass = typeof(CompAbilityEffect_RockToss);
        }
    }

    public class CompAbilityEffect_RockToss : CompAbilityEffect_WithDest, ITargetingSource
    {
        public new CompProperties_AbilityRockToss Props => (CompProperties_AbilityRockToss)props;
     
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // TODO
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = false)
        {
            if (target.Thing?.def.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) != true)
                return false;

            return base.Valid(target, showMessages);
        }

        public override bool CanHitTarget(LocalTargetInfo target)
        {
            if (!CanPlaceSelectedTargetAt(target))
                return false;
            if (target.Cell.DistanceTo(selectedTarget.Cell) < Props.minRange)
                return false;

            return base.CanHitTarget(target);
        }

        void ITargetingSource.DrawHighlight(LocalTargetInfo target)
        {
            if (Props.range > 0f)
            {
                GenDraw.DrawRadiusRing(selectedTarget.Cell, Props.range, Color.white, c => c.DistanceTo(selectedTarget.Cell) >= Props.minRange);
            }
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
    }
}
