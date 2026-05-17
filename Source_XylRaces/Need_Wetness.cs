using RimWorld;
using RimWorld.Planet;
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
        public bool ShouldFulfill => CurLevel <= 0.67f;

        private float TemperatureFactor => TemperatureWetnessFallFactorCurve.Evaluate(pawn.AmbientTemperature);

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

        private static readonly SimpleCurve RainfallToWetnessCurve =
        [
            new CurvePoint(500f, 0.0f),
            new CurvePoint(1500f, 0.3f),
            new CurvePoint(2500f, 1.0f),
        ];

        private int lastInstantWetnessCheckTick;
        private float lastInstantWetness;

        public override float CurInstantLevel
        {
            get
            {
                if (lastInstantWetnessCheckTick == Find.TickManager.TicksGame)
                    return lastInstantWetness;

                lastInstantWetnessCheckTick = Find.TickManager.TicksGame;
                lastInstantWetness = CalculateInstantWetness();

                return lastInstantWetness;
            }
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

        private float CalculateInstantWetness()
        {
            if (pawn.IsInCaravan())
            {
                var caravan = pawn.GetCaravan();
                var tile = Find.WorldGrid[caravan.Tile];
                if (tile.IsCoastalOrRiverTile())
                    return 1.0f;
                if (tile.IsWetlandBiome())
                    return 1.0f;
                return RainfallToWetnessCurve.Evaluate(tile.rainfall);
            }

            if (!pawn.Spawned)
            {
                return 0.0f;
            }

            if (Config.Instance.wetnessGivingJobs.Contains(pawn.CurJobDef) && !pawn.pather.Moving)
            {
                var wetnessSource = pawn.CurJob?.targetA.Thing?.def.GetModExtension<ThingDefExtension_WetnessSource>();
                return wetnessSource?.wetnessLevel ?? 1.0f;
            }

            return GetWetness(pawn.Position, pawn.Map);
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

        public override void DrawOnGUI(
            Rect rect,
            int maxThresholdMarkers = 2147483647,
            float customMargin = -1,
            bool drawArrows = true,
            bool doTooltip = true,
            Rect? rectForTooltip = null,
            bool drawLabel = true)
        {
            threshPercents ??= [thresholdVeryDry, thresholdDry, thresholdNeutral, thresholdWet];
            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip, rectForTooltip, drawLabel);
        }
    }
}
