using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Torpor : GeneDefExtension
    {
        public HediffDef hediff;
        public float severityGainPerDay;
        public float severityLossPerDay;
        public string warningMessage;
    }

    [UsedImplicitly]
    public class Torpor : Gene
    {
        public GeneDefExtension_Torpor DefExt => def.GetModExtension<GeneDefExtension_Torpor>();

        private bool sentWarning = false;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref sentWarning, nameof(sentWarning));
        }

        public override void TickInterval(int delta)
        {
            const int checkInterval = 60;

            using (new ProfileBlock())
            {
                base.TickInterval(delta);

                if (!Active)
                    return;

                if (!pawn.IsHashIntervalTick(checkInterval, delta))
                    return;

                if (pawn.AmbientTemperature < pawn.GetStatValue(StatDefOf.ComfyTemperatureMin))
                {
                    HealthUtility.AdjustSeverity(pawn, DefExt.hediff, (checkInterval / (float)GenDate.TicksPerDay) * DefExt.severityGainPerDay);
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, DefExt.hediff, -(checkInterval / (float)GenDate.TicksPerDay) * DefExt.severityLossPerDay);
                }

                Hediff torpor = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediff);

                if (!sentWarning && !DefExt.warningMessage.NullOrEmpty() && pawn.IsPlayerControlled && torpor?.Visible == true)
                {
                    Messages.Message(DefExt.warningMessage.Formatted(pawn.Named("PAWN")), pawn,
                        MessageTypeDefOf.NegativeHealthEvent);
                    sentWarning = true;
                }

                if (sentWarning && (torpor?.Severity ?? 0) <= 0)
                    sentWarning = false;

                if ((torpor?.CurStageIndex ?? 0) >= 3)
                    pawn.needs.rest.CurLevelPercentage = Mathf.Min(pawn.needs.rest.CurLevelPercentage, 0.1f);
            }
        }
    }
}
