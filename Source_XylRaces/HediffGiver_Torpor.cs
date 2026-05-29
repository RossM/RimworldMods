using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class HediffGiver_Torpor : HediffGiver
    {
        public float temperatureThreshold;
        public float comfyTemperatureImportance;
        public float severityGainPerDayPerDegree;
        public float severityLossPerDayPerDegree;

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            float minimumTemperature = Mathf.Lerp(temperatureThreshold,
                pawn.GetStatValue(StatDefOf.ComfyTemperatureMin), comfyTemperatureImportance);

            float temperatureDifference = minimumTemperature - pawn.AmbientTemperature;

            float changePerDay = temperatureDifference *
                                 (temperatureDifference > 0 ? severityGainPerDayPerDegree : severityLossPerDayPerDegree);

            HealthUtility.AdjustSeverity(pawn, hediff, 60f / GenDate.TicksPerDay * changePerDay);
        }
    }
}
