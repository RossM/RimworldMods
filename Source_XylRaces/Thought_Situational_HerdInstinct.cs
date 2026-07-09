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

        int colonistCount;

        if (pawn.Map is { } map)
            colonistCount = map.mapPawns.ColonistsSpawnedCount;
        else if (pawn.GetCaravan() is { } caravan)
            colonistCount = caravan.PlayerPawnsForStoryteller.Count();
        else
            colonistCount = 1;

        return MoodOffsetCurveFromPopulation.Evaluate(colonistCount);
    }
}
