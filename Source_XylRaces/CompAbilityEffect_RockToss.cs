using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class CompAbilityEffect_RockToss : CompAbilityEffect_WithDest
    {
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

            return base.CanHitTarget(target);
        }
    }
}
