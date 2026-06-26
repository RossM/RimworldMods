using RimWorld.Planet;

namespace XylXenos;

public enum WetnessCategory : byte
{
    Parched,
    VeryDry,
    Dry,
    Neutral,
    Wet,
}

public class Need_Wetness : Need_Seeker
{
    public float TemperatureFactor => TemperatureWetnessFallFactorCurve.Evaluate(pawn.AmbientTemperature);

    public float RisePerHour => def.seekerRisePerHour;
    public float FallPerHour => def.seekerFallPerHour * TemperatureFactor;

    public const float thresholdWet = 0.90f;
    public const float thresholdNeutral = 0.50f;
    public const float thresholdDry = 0.25f;
    public const float thresholdVeryDry = 0.05f;

    private static readonly SimpleCurve TemperatureWetnessFallFactorCurve =
    [
        new(-19.0f, 1.0f),
        new(6.0f, 0.5f),
        new(16.0f, 1.0f),
        new(21.0f, 1.0f),
        new(31.0f, 2.0f),
        new(41.0f, 5.0f)
    ];

    private static readonly SimpleCurve RainfallToWetnessCurve =
    [
        new(500f, 0.0f),
        new(1500f, 0.3f),
        new(2500f, 1.0f),
    ];

    private int lastInstantWetnessCheckTick;

    public Need_Wetness(Pawn pawn) : base(pawn)
    {
        threshPercents = [thresholdVeryDry, thresholdDry, thresholdNeutral, thresholdWet];
    }

    public override float CurInstantLevel
    {
        get
        {
            if (lastInstantWetnessCheckTick == Find.TickManager.TicksGame)
                return field;

            lastInstantWetnessCheckTick = Find.TickManager.TicksGame;
            field = CalculateInstantWetness();

            return field;
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
            if (tile.IsCoastalOrRiverTile)
                return 1.0f;
            if (tile.IsWetlandBiome)
                return 1.0f;
            return RainfallToWetnessCurve.Evaluate(tile.rainfall);
        }

        if (!pawn.Spawned)
        {
            return 0.0f;
        }

        if (Config.Instance.wetnessGivingJobs.Contains(pawn.CurJobDef) && !pawn.pather.Moving)
        {
            var wetnessSource = pawn.CurJob?.targetA.Thing?.def.GetModExtension<DefModExtension_Thing_WetnessSource>();
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
            CurLevel += RisePerHour * 0.06f;
            CurLevel = Mathf.Min(CurLevel, curInstantLevel);
        }
        else if (curInstantLevel < CurLevel)
        {
            CurLevel -= FallPerHour * 0.06f;
            CurLevel = Mathf.Max(CurLevel, curInstantLevel);
        }
    }

    public override string GetTipString()
    {
        float ambientTemperature = pawn.AmbientTemperature;
        float temperatureFactor = TemperatureWetnessFallFactorCurve.Evaluate(ambientTemperature);
        float hoursPerDay = FallPerHour * 24.0f / (FallPerHour + RisePerHour);
        return base.GetTipString() + "\n\n" + "XylWetnessNeedModifiedByTemperature".Translate(
                pawn.Named("PAWN"),
                ambientTemperature.ToStringTemperature().Named("TEMPERATURE"),
                temperatureFactor.ToStringPercent().Named("FACTOR"),
                hoursPerDay.ToStringDecimalIfSmall().Named("HOURS"))
            ;
    }
}
