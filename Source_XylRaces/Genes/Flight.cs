﻿using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Flight : DefModExtension_WithIcon
    {
        public float autoFlyMinDistance = 25f;
    }

    public class Flight : Gene
    {
        public bool autoFly = true;

        [Unsaved()] 
        private bool wasFlying;

        public GeneDefExtension_Flight DefExt => def.GetModExtension<GeneDefExtension_Flight>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref autoFly, nameof(autoFly));
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
                yield break;
            if (!pawn.Spawned)
                yield break;
            if (!pawn.IsColonistPlayerControlled)
                yield break;

            yield return new Command_ActionWithCooldown()
            {
                action = () =>
                {
                    pawn.flight.StartFlying();
                },
                defaultLabel = "XylCommandFlyLabel".TranslateSimple(),
                defaultDesc = "XylCommandFlyDesc".TranslateSimple(),
                Disabled = !pawn.flight.CanFlyNow,
                cooldownPercentGetter = () => 1.0f - pawn.flight.Get<int>("flightCooldownTicks") / (pawn.GetStatValue(StatDefOf.FlightCooldown) * 60f),
                icon = DefExt.Icon,
                defaultDescPostfix = "\n\n" + 
                                     "CooldownTime".TranslateSimple() + ": " + pawn.GetStatValue(StatDefOf.FlightCooldown).ToStringDecimalIfSmall() + "LetterSecond".TranslateSimple() + "\n" + 
                                     "AbilityDuration".TranslateSimple() + ": " + pawn.GetStatValue(StatDefOf.MaxFlightTime).ToStringDecimalIfSmall() + "LetterSecond".TranslateSimple(),
            };

            yield return new Command_Toggle
            {
                defaultLabel = "XylCommandAutoFlyLabel".TranslateSimple(),
                defaultDesc = "XylCommandAutoFlyDesc".TranslateSimple(),
                isActive = () => autoFly,
                toggleAction = () => { autoFly = !autoFly; },
                icon = DefExt.Icon,
            };
        }

        public override void Tick()
        {
            using (new ProfileBlock())
            {
                base.Tick();

                Pawn_FlightTracker flight = pawn.flight;
                if (flight == null)
                    return;

                if (flight.Flying != wasFlying)
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                    wasFlying = flight.Flying;
                }

                if (!flight.CanEverFly)
                    return;

                if (!flight.Flying && autoFly && pawn.pather.Moving &&
                    pawn.Position.DistanceTo(pawn.pather.Destination.Cell) >= DefExt.autoFlyMinDistance &&
                    pawn.CurJob?.locomotionUrgency > LocomotionUrgency.Walk)
                {
                    flight.StartFlying();
                }
            }
        }

        // If a downed flying pawn lands on a non-walkable tile, they are killed and their corpse destroyed.
        // This would be unfortunate, so try to move the pawn to a better position.
        public void Notify_Downed()
        {
            var newCell = CellFinder.StandableCellNear(pawn.Position, pawn.Map, 5f);
            if (newCell != IntVec3.Invalid)
                pawn.Position = newCell;
        }
    }
}
