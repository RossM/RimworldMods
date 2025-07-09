using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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

        [DefOf]
        private static class Defs
        {
            [UsedImplicitly, MayRequire("Xylthixlm.Races.Nixie")]
            public static JobDef XylTakeShower;
        }

        public override float CurInstantLevel
        {
            get
            {
                if (lastInstantWetnessCheckTick == Find.TickManager.TicksGame)
                    return lastInstantWetness;
                lastInstantWetnessCheckTick = Find.TickManager.TicksGame;

                if (!pawn.Spawned)
                    lastInstantWetness = 0.0f;
                else
                {
                    TerrainDef terrain = pawn.Position.GetTerrain(pawn.Map);
                    WeatherDef curWeatherLerped = pawn.Map.weatherManager.CurWeatherLerped;

                    if (pawn.CurJobDef == Defs.XylTakeShower && pawn.Position == pawn.CurJob.GetTarget(TargetIndex.A).Cell)
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