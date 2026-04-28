using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Torpor : DefModExtension
    {
        public HediffDef hediff;
        public float severityGainPerDay;
        public float severityLossPerDay;
    }

    [UsedImplicitly]
    public class Torpor : Gene
    {
        public GeneDefExtension_Torpor DefExt => def.GetModExtension<GeneDefExtension_Torpor>();

        public override void TickInterval(int delta)
        {
            const int tickInterval = 60;

            using (new ProfileBlock())
            {
                base.TickInterval(delta);

                if (!Active)
                    return;

                if (!pawn.IsHashIntervalTick(tickInterval, delta))
                    return;

                if (pawn.AmbientTemperature < pawn.GetStatValue(StatDefOf.ComfyTemperatureMin))
                {
                    HealthUtility.AdjustSeverity(pawn, DefExt.hediff, (tickInterval / 60000f) * DefExt.severityGainPerDay);
                }
                else
                {
                    HealthUtility.AdjustSeverity(pawn, DefExt.hediff, -(tickInterval / 60000f) * DefExt.severityLossPerDay);
                }
            }
        }
    }
}
