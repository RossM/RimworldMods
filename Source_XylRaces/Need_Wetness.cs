using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public enum WetnessCategory : byte
    {
        Parched,
        VeryDry,
        Dry,
        Neutral,
        Wet,
    }

    public class Need_Wetness(Pawn pawn) : Need_Seeker(pawn)
    {
        private int lastInstantWetnessCheckTick;
        private float lastInstantWetness;

        private const float thresholdWet = 0.90f;
        private const float thresholdNeutral = 0.50f;
        private const float thresholdDry = 0.25f;
        private const float thresholdVeryDry = 0.05f;

        private static readonly SimpleCurve TemperatureWetnessFallFactorCurve =
        [
            new CurvePoint(-19.0f, 1.0f),
            new CurvePoint(6.0f, 0.5f),
            new CurvePoint(16.0f, 1.0f),
            new CurvePoint(21.0f, 1.0f),
            new CurvePoint(31.0f, 2.0f),
            new CurvePoint(41.0f, 5.0f)
        ];

        public override float CurInstantLevel
        {
            get
            {
                var wetnessGivingJobs = Config.Instance.wetnessGivingJobs;

                if (lastInstantWetnessCheckTick == Find.TickManager.TicksGame)
                    return lastInstantWetness;
                lastInstantWetnessCheckTick = Find.TickManager.TicksGame;

                if (!pawn.Spawned)
                    lastInstantWetness = 0.0f;
                else if (wetnessGivingJobs.Contains(pawn.CurJobDef) && !pawn.pather.Moving)
                    lastInstantWetness = 1.0f;
                else
                    lastInstantWetness = GetWetness(pawn.Position, pawn.Map);

                return lastInstantWetness;
            }
        }

        public static float GetWetness(IntVec3 position, Map map)
        {
            TerrainDef terrain = position.GetTerrain(map);
            WeatherDef curWeatherLerped = map.weatherManager.CurWeatherLerped;

            if (terrain.IsWater)
                return 1.0f;
            if (position.GetThingList(map).Any(t => t.def == ThingDefOf.Filth_Water))
                return 1.0f;
            if (!position.Roofed(map))
                return Mathf.Clamp01(curWeatherLerped.rainRate / 0.25f);
            return 0.0f;
        }

        public WetnessCategory CurCategory
        {
            get
            {
                return CurLevel switch
                {
                    >= thresholdWet => WetnessCategory.Wet,
                    >= thresholdNeutral => WetnessCategory.Neutral,
                    >= thresholdDry => WetnessCategory.Dry,
                    >= thresholdVeryDry => WetnessCategory.VeryDry,
                    _ => WetnessCategory.Parched
                };
            }
        }

        public bool ShouldFulfill => CurLevel <= 0.67f;

        public override void NeedInterval()
        {
            if (IsFrozen) 
                return;

            float curInstantLevel = CurInstantLevel;
            if (curInstantLevel > CurLevel)
            {
                CurLevel += def.seekerRisePerHour * 0.06f;
                CurLevel = Mathf.Min(CurLevel, curInstantLevel);
            }
            else if (curInstantLevel < CurLevel)
            {
                CurLevel -= def.seekerFallPerHour * TemperatureFactor * 0.06f;
                CurLevel = Mathf.Max(CurLevel, curInstantLevel);
            }
        }

        private float TemperatureFactor => TemperatureWetnessFallFactorCurve.Evaluate(pawn.AmbientTemperature);

        public override string GetTipString()
        {
            float ambientTemperature = pawn.AmbientTemperature;
            float temperatureFactor = TemperatureWetnessFallFactorCurve.Evaluate(ambientTemperature);
            float modifiedFallRate = def.seekerFallPerHour * temperatureFactor;
            float hoursPerDay = modifiedFallRate * 24.0f / (modifiedFallRate + def.seekerRisePerHour);
            return base.GetTipString() + "\n\n" + "XylWetnessNeedModifiedByTemperature".Translate(
                    pawn.Named("PAWN"),
                    ambientTemperature.ToStringTemperature().Named("TEMPERATURE"),
                    temperatureFactor.ToStringPercent().Named("FACTOR"),
                    hoursPerDay.ToStringDecimalIfSmall().Named("HOURS"))
                ;
        }

        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = 2147483647, float customMargin = -1, bool drawArrows = true,
            bool doTooltip = true, Rect? rectForTooltip = null, bool drawLabel = true)
        {
            threshPercents ??= [thresholdVeryDry, thresholdDry, thresholdNeutral, thresholdWet];
            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip, rectForTooltip, drawLabel);
        }
    }
}