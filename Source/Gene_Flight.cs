using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    public class GeneDefExtension_Flight : DefModExtension
    {
        [NoTranslate]
        public string iconPath;

        [Unsaved(false)]
        private Texture2D cachedIcon;

        public Texture2D Icon
        {
            get
            {
                cachedIcon ??= iconPath.NullOrEmpty()
                    ? BaseContent.BadTex
                    : ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
                return cachedIcon;
            }
        }
    }

    public class Gene_Flight : Gene
    {
        public bool autoFly = false;

        [Unsaved(false)] private bool wasFlying;

        public GeneDefExtension_Flight DefExt => def.GetModExtension<GeneDefExtension_Flight>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref autoFly, "autoFly", false);
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
                defaultLabel = "Fly",
                defaultDesc = "Start flying. Flying grants increased movement speed and allows travelling over some obstacles.",
                Disabled = !pawn.flight.CanFlyNow,
                cooldownPercentGetter = () => 1.0f - pawn.flight.Get<int>("flightCooldownTicks") / (pawn.GetStatValue(StatDefOf.FlightCooldown) * 60f),
                icon = DefExt.Icon,
                defaultDescPostfix = "\n\nCooldown: " + pawn.GetStatValue(StatDefOf.FlightCooldown).ToStringDecimalIfSmall() + "s\nEffect duration: " + pawn.GetStatValue(StatDefOf.MaxFlightTime).ToStringDecimalIfSmall() + "s",
            };

            yield return new Command_Toggle
            {
                defaultLabel = "Auto-fly",
                defaultDesc = "The character will automatically start flying if they are moving and the fly ability is off cooldown.",
                isActive = () => autoFly,
                toggleAction = () => { autoFly = !autoFly; },
                icon = DefExt.Icon,
            };
        }

        public void Notify_JobStarted(Job job)
        {

        }

        public override void Tick()
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

            if (!pawn.IsPlayerControlled)
                return;
            if (!flight.CanEverFly)
                return;

            if (!flight.Flying && autoFly && pawn.pather.Moving && pawn.CurJob?.locomotionUrgency > LocomotionUrgency.Walk)
                flight.StartFlying();

            // Workaround for bug in `Pawn_FlightTracker`
            if (!flight.Flying)
                pawn.flight.Set<int>("flyingTicks", 0);
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
