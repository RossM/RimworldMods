using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public class Thought_Situational_HerdInstinct : Thought_Situational
    {
        public const int NumPawns_Alone = 1;
        public const int NumPawns_SmallHerd = 5;
        public const int NumPawns_Inactive = 12;
        public const int NumPawns_LargeHerd = 15;

        private static readonly SimpleCurve MoodOffsetCurveFromPopulation =
        [
            new CurvePoint(1f, -15f),
            new CurvePoint(2f, -12f),
            new CurvePoint(3f, -9f),
            new CurvePoint(4f, -6f),
            new CurvePoint(5f, -3f),
            new CurvePoint(6f, 0f),
            new CurvePoint(12f, 0f),
            new CurvePoint(13f, 1f),
            new CurvePoint(14f, 2f),
            new CurvePoint(15f, 3f),
            new CurvePoint(16f, 4f),
            new CurvePoint(17f, 5f),
            new CurvePoint(18f, 6f),
        ];

        public override float MoodOffset()
        {
            int colonistCount = pawn.Map.mapPawns.ColonistsSpawnedCount;
            return MoodOffsetCurveFromPopulation.Evaluate(colonistCount);
        }
    }
}

