using RimWorld.Planet;

namespace XylXenos;

[UsedFromXml]
public class Thought_Situational_HerdInstinct : Thought_Situational
{
    public const int NumPawns_Alone = 1;
    public const int NumPawns_SmallHerd = 5;
    public const int NumPawns_Inactive = 12;
    public const int NumPawns_LargeHerd = 15;

    private static readonly SimpleCurve MoodOffsetCurveFromPopulation =
    [
        new(1f, -15f),
        new(6f, 0f),
        new(12f, 0f),
        new(18f, 6f),
    ];

    public override float MoodOffset()
    {
        DebugAssert.NotNull(pawn);

        int colonistCount = ColonistCount(pawn);

        return MoodOffsetCurveFromPopulation.Evaluate(colonistCount);
    }

    public static int ColonistCount(Pawn pawn)
    {
        if (pawn.Map is { } map)
            return map.mapPawns.ColonistsSpawnedCount;
        if (pawn.GetCaravan() is { } caravan)
            return caravan.PlayerPawnsForStoryteller.Count(p => p.IsColonist);
        return 1;
    }
}
