using System.Collections.Generic;
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
        public ThingDef projectileDef;

        public CompProperties_AbilityRockToss()
        {
            compClass = typeof(CompAbilityEffect_RockToss);
        }
    }

    public class CompAbilityEffect_RockToss : CompAbilityEffect_WithDest, ITargetingSource
    {
        public new CompProperties_AbilityRockToss Props => (CompProperties_AbilityRockToss)props;

        public LocalTargetInfo SelectedTarget => selectedTarget;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            // During targeting, there are two targets. However, when we actually get activated by
            // JobDriver_CastWithHeldThing, the destination (target B) becomes the primary target. Otherwise
            // the targeter would abort because target A is in our inventory and no longer spawned.
            base.Apply(target, dest);
            Log.Message($"target={target}, dest={dest}, CarriedThing={parent.pawn.carryTracker.CarriedThing}");
            if (parent.pawn.carryTracker.CarriedThing != null)
                LaunchProjectile(target);
        }

        private void LaunchProjectile(LocalTargetInfo target)
        {
            if (Props.projectileDef != null)
            {
                Pawn pawn = parent.pawn;
                Projectile projectile = (Projectile)GenSpawn.Spawn(Props.projectileDef, pawn.Position, pawn.Map);
                Log.Message($"projectile={projectile} ({projectile.GetType()}");
                Thing thing = pawn.carryTracker.CarriedThing;
                if (projectile.GetComp<CompThingContainer>()?.innerContainer.TryAddOrTransfer(thing) != true)
                {
                    Log.Warning("Failed to add thing to projectile: projectile={projectile} thing={thing}");
                    return;
                }
                projectile.Launch(pawn, pawn.DrawPos, target, target, ProjectileHitFlags.IntendedTarget, parent.verb.preventFriendlyFire);
            }
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
            if (!GenSight.LineOfSight(selectedTarget.Cell, target.Cell, parent.pawn.Map))
                return false;

            return base.CanHitTarget(target);
        }

        void ITargetingSource.DrawHighlight(LocalTargetInfo target)
        {
            if (Props.range > 0f)
            {
                GenDraw.DrawRadiusRing(selectedTarget.Cell, Props.range, Color.white, c => c.DistanceTo(selectedTarget.Cell) >= Props.minRange && GenSight.LineOfSight(selectedTarget.Cell, c, parent.pawn.Map));
            }
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
    }
}
