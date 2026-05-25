using Verse;

namespace XylXenos;

public static class PawnHelpers
{
    public static LookupCache LookupCache(this Pawn pawn)
    {
        return XylXenos.LookupCache.Tracker.Get(pawn);
    }
}
