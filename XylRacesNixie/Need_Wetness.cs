using RimWorld;
using Verse;

namespace XylRacesNixie
{
    public class Need_Wetness : Need_Seeker
    {
        private int lastInstantWetnessCheckTick;
        private float lastInstantWetness;

        public Need_Wetness(Pawn pawn) : base(pawn)
        {
        }

        public override float CurInstantLevel {
            get
            {
                if (lastInstantWetnessCheckTick == Find.TickManager.TicksGame) return lastInstantWetness;
                lastInstantWetnessCheckTick = Find.TickManager.TicksGame;

                if (!this.pawn.Spawned)
                    lastInstantWetness = 0.0f;
                else
                {
                    TerrainDef terrain = this.pawn.Position.GetTerrain(this.pawn.Map);
                    WeatherDef curWeatherLerped = this.pawn.Map.weatherManager.CurWeatherLerped;
                    if (terrain.traversedThought?.defName == "SoakingWet")
                        lastInstantWetness = 1.0f;
                    else if (curWeatherLerped.weatherThought?.defName == "SoakingWet" &&
                             !this.pawn.Position.Roofed(this.pawn.Map))
                        lastInstantWetness = 1.0f;
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
                if (CurLevel >= 0.90f)
                    return WetnessCategory.Wet;
                if (CurLevel >= 0.67f)
                    return WetnessCategory.Neutral;
                if (CurLevel >= 0.34f)
                    return WetnessCategory.Dry;
                if (CurLevel >= 0.01f)
                    return WetnessCategory.VeryDry;
                return WetnessCategory.Parched;
            }
        }
    }
}
