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

        public bool IsShowering { get; set; }

        public override float CurInstantLevel
        {
            get
            {
                if (lastInstantWetnessCheckTick == Find.TickManager.TicksGame)
                    return lastInstantWetness;
                lastInstantWetnessCheckTick = Find.TickManager.TicksGame;

                if (IsShowering && pawn.CurJob?.GetCachedDriver(pawn) is not JobDriver_TakeShower)
                    IsShowering = false;

                if (!pawn.Spawned)
                    lastInstantWetness = 0.0f;
                else
                {
                    TerrainDef terrain = pawn.Position.GetTerrain(pawn.Map);
                    WeatherDef curWeatherLerped = pawn.Map.weatherManager.CurWeatherLerped;

                    if (IsShowering)
                        lastInstantWetness = 1.0f;
                    else if (terrain.IsWater)
                        lastInstantWetness = 1.0f;
                    else if (!pawn.Position.Roofed(pawn.Map))
                        lastInstantWetness = Mathf.Clamp01(curWeatherLerped.rainRate / 0.25f);
                    else
                        lastInstantWetness = 0.0f;
                }

                return lastInstantWetness;
            }
        }

        public WetnessCategory CurCategory
        {
            get
            {
                return CurLevel switch
                {
                    >= 0.90f => WetnessCategory.Wet,
                    >= 0.67f => WetnessCategory.Neutral,
                    >= 0.34f => WetnessCategory.Dry,
                    >= 0.01f => WetnessCategory.VeryDry,
                    _ => WetnessCategory.Parched
                };
            }
        }
    }
}