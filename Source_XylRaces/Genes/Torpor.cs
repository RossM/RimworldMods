using JetBrains.Annotations;
using RimWorld;
using Verse;
using static UnityEngine.GraphicsBuffer;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Torpor : DefModExtension
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
                    HealthUtility.AdjustSeverity(pawn, DefExt.hediff, (checkInterval / 60000f) * DefExt.severityGainPerDay);

                    if (!sentWarning && !DefExt.warningMessage.NullOrEmpty() && pawn.IsPlayerControlled && 
                        pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediff)?.Visible == true)
                    {
                        Messages.Message(DefExt.warningMessage.Formatted(pawn.Named("PAWN")), pawn,
                            MessageTypeDefOf.NegativeHealthEvent);
                        sentWarning = true;
                    }
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, DefExt.hediff, -(checkInterval / 60000f) * DefExt.severityLossPerDay);

                    if (sentWarning && (pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediff)?.Severity ?? 0) <= 0)
                        sentWarning = false;
                }
            }
        }
    }
}
