using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
    public class TorporInfo
    {
        public HediffDef hediff;
        public float temperatureThreshold;
        public float comfyTemperatureImportance;
        public float severityGainPerDayPerDegree;
        public float severityLossPerDayPerDegree;
    }

    [UsedImplicitly]
    public class Torpor : GeneExt
    {
        [NotNull] public TorporInfo TorporInfo => DefExt.torpor!;

        public override void TickInterval(int delta)
        {
            const int checkInterval = 60;

            base.TickInterval(delta);

            if (!Active)
                return;

            if (!pawn.IsHashIntervalTick(checkInterval, delta))
                return;

            float minimumTemperature = Mathf.Lerp(TorporInfo.temperatureThreshold,
                pawn.GetStatValue(StatDefOf.ComfyTemperatureMin), TorporInfo.comfyTemperatureImportance);
            float temperatureDifference = minimumTemperature - pawn.AmbientTemperature;
            float changePerDay = temperatureDifference * (temperatureDifference > 0
                ? TorporInfo.severityGainPerDayPerDegree
                : TorporInfo.severityLossPerDayPerDegree);
            HealthUtility.AdjustSeverity(pawn, TorporInfo.hediff, (checkInterval / (float)GenDate.TicksPerDay) * changePerDay);

            Hediff torpor = pawn.health.hediffSet.GetFirstHediffOfDef(TorporInfo.hediff);

            if ((torpor?.CurStageIndex ?? 0) >= 3)
                pawn.needs.rest.CurLevelPercentage = Mathf.Min(pawn.needs.rest.CurLevelPercentage, 0.1f);
        }
    }
}
