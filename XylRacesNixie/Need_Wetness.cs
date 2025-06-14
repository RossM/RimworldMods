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
                if (lastInstantWetnessCheckTick != Find.TickManager.TicksGame)
                {
                    TerrainDef terrain = this.pawn.Position.GetTerrain(this.pawn.Map);
                    WeatherDef curWeatherLerped = this.pawn.Map.weatherManager.CurWeatherLerped;
                    if (terrain.traversedThought?.defName == "SoakingWet")
                        lastInstantWetness = 1.0f;
                    else if (curWeatherLerped.weatherThought?.defName == "SoakingWet")
                        lastInstantWetness = 1.0f;
                    else
                        lastInstantWetness = 0;

                    lastInstantWetnessCheckTick = Find.TickManager.TicksGame;
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
