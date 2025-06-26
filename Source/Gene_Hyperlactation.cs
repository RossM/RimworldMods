using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public class Gene_Hyperlactation : Gene
    {
        public bool allowMilking;

        public int? fullSinceTick;

        public float ChargePerItem => 0.1f;

        private static HediffDef HyperlactatingHediff;

        public HediffComp_Lactating LactationCharge => GetPawnLactationHediff(pawn).TryGetComp<HediffComp_Lactating>();

        public ThoughtDef fullThoughtDef => DefDatabase<ThoughtDef>.GetNamed("SoreBreasts");

        public IEnumerable<ThoughtDef> milkedThoughts =
        [
            DefDatabase<ThoughtDef>.GetNamed("Milked"),
            DefDatabase<ThoughtDef>.GetNamed("MilkedMood"),
            DefDatabase<ThoughtDef>.GetNamed("Milked_Masochist"),
            DefDatabase<ThoughtDef>.GetNamed("MilkedMood_Masochist"),
        ];

        public override bool Active
        {
            get
            {
                if (!base.Active)
                    return false;
                return pawn?.gender == Gender.Female;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fullSinceTick, "fullSinceTick");
            Scribe_Values.Look(ref allowMilking, "allowMilking");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            if (!Active)
                yield break;
            if (!pawn.Spawned)
                yield break;

            yield return new Command_Toggle
            {
                defaultLabel = "Allow milking",
                defaultDesc = "Allow other characters to milk this character when she has enough milk.",
                isActive = () => allowMilking,
                toggleAction = () => { allowMilking = !allowMilking; }
            };
        }

        public override void PostAdd()
        {
            base.PostAdd();

            AddHediff();
        }

        public override void TickInterval(int delta)
        {
            if (!Active)
                return;

            base.TickInterval(delta);

            if (pawn.IsHashIntervalTick(60, delta))
            {
                AddHediff();

                if (LactationCharge != null && LactationCharge.Charge >= LactationCharge.Props.fullChargeAmount)
                {
                    //Log.Message(string.Format("fullSinceTick: {0}", fullSinceTick));
                    fullSinceTick ??= Find.TickManager.TicksGame;
                }
                else
                    fullSinceTick = null;
            }
        }

        private void AddHediff()
        {
            if (!Active)
                return;

            HyperlactatingHediff ??= DefDatabase<HediffDef>.GetNamed("Hyperlactating");

            if (pawn.health.hediffSet.HasHediff(HediffDefOf.Malnutrition))
                return;

            var lactatingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
            if (lactatingHediff != null)
                pawn.health.RemoveHediff(lactatingHediff);

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(HyperlactatingHediff) ??
                            pawn.health.AddHediff(HyperlactatingHediff);
            hediff.Severity = 1.0f;
        }

        public static Hediff GetPawnLactationHediff(Pawn pawn)
        {
            PatchLactation.Hyperlactating ??= DefDatabase<HediffDef>.GetNamed("Hyperlactating");

            return pawn.health.hediffSet.GetFirstHediffOfDef(PatchLactation.Hyperlactating) ??
                   pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
        }

        public static bool HasPawnLactationHediff(Pawn pawn)
        {
            return GetPawnLactationHediff(pawn) != null;
        }

        public int MilkCount => Mathf.FloorToInt((this.LactationCharge?.Charge ?? 0) / this.ChargePerItem);
    }
}
