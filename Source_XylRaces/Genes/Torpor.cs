using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
    public class GeneDefExtension_Torpor : GeneDefExtension
    {
        public HediffDef hediff;
        public float temperatureThreshold;
        public float comfyTemperatureImportance;
        public float severityGainPerDayPerDegree;
        public float severityLossPerDayPerDegree;
    }

    [UsedImplicitly]
    public class Torpor : Gene
    {
        public GeneDefExtension_Torpor DefExt => def.GetModExtension<GeneDefExtension_Torpor>();

        public override void TickInterval(int delta)
        {
            const int checkInterval = 60;

            base.TickInterval(delta);

            if (!Active)
                return;

            if (!pawn.IsHashIntervalTick(checkInterval, delta))
                return;

            float minimumTemperature = Mathf.Lerp(DefExt.temperatureThreshold,
                pawn.GetStatValue(StatDefOf.ComfyTemperatureMin), DefExt.comfyTemperatureImportance);
            float temperatureDifference = minimumTemperature - pawn.AmbientTemperature;
            float changePerDay = temperatureDifference * (temperatureDifference > 0
                ? DefExt.severityGainPerDayPerDegree
                : DefExt.severityLossPerDayPerDegree);
            HealthUtility.AdjustSeverity(pawn, DefExt.hediff, (checkInterval / (float)GenDate.TicksPerDay) * changePerDay);

            Hediff torpor = pawn.health.hediffSet.GetFirstHediffOfDef(DefExt.hediff);

            if ((torpor?.CurStageIndex ?? 0) >= 3)
                pawn.needs.rest.CurLevelPercentage = Mathf.Min(pawn.needs.rest.CurLevelPercentage, 0.1f);
        }
    }
}
