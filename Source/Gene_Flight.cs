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
        public bool allowFlight = false;
        public bool flyOnlyWhenDrafted = false;

        [Unsaved(false)] private bool wasFlying;

        public GeneDefExtension_Flight DefExt => def.GetModExtension<GeneDefExtension_Flight>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowFlight, "allowFlight", false);
            Scribe_Values.Look(ref flyOnlyWhenDrafted, "flyOnlyWhenDrafted", false);
        }


        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
                yield break;
            if (!pawn.Spawned)
                yield break;
            if (!pawn.IsColonistPlayerControlled)
                yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "Allow flight",
                defaultDesc = "Allow this character to fly.",
                isActive = () => allowFlight,
                toggleAction = () => { allowFlight = !allowFlight; },
                icon = DefExt.Icon,
            };

            if (allowFlight)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "Fly only when drafted",
                    defaultDesc = "Restrict this character to only fly when drafted.",
                    isActive = () => flyOnlyWhenDrafted,
                    toggleAction = () => { flyOnlyWhenDrafted = !flyOnlyWhenDrafted; },
                    icon = DefExt.Icon,
                };
            }
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

            bool tryFlying = allowFlight && (!flyOnlyWhenDrafted || pawn.Drafted);
            if (flight.Flying)
            {
                if (!tryFlying)
                    flight.ForceLand();;
            }
            else
            {
                if (tryFlying)
                    flight.StartFlying();
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
