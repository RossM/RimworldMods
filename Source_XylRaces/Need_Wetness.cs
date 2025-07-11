using System.Linq;
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

        public override float CurInstantLevel
        {
            get
            {
                var wetnessGivingJobs = DefDatabase<Config>.AllDefs.FirstOrDefault()?.wetnessGivingJobs ?? [];

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