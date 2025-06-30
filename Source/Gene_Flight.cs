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

        public GeneDefExtension_Flight DefExt => def.GetModExtension<GeneDefExtension_Flight>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref allowFlight, "allowFlight", false);
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
        }

        public void Notify_JobStarted(Job job)
        {

        }

        public override void Tick()
        {
            base.Tick();

            if (!pawn.IsPlayerControlled)
                return;

            Log.Message(string.Format("Flight: {0} {1}", pawn.flight?.CanEverFly, pawn.flight?.Flying));

            Pawn_FlightTracker flight = pawn.flight;
            if (flight == null)
                return;
            if (!flight.CanEverFly)
                return;

            bool tryFlying = allowFlight && (pawn.CurJob?.def.tryStartFlying != false || flight.Flying);
            //bool tryFlying = allowFlight;
            if (flight.Flying)
            {
                if (!tryFlying)
                    flight.ForceLand();;
            }
            else
            {
                if (tryFlying)
                {
                    Log.Message(string.Format("{0}: Taking off", pawn));
                    flight.StartFlying();
                }
            }
        }
    }
}
