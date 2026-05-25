using System.Runtime.CompilerServices;
using Verse;

namespace XylXenos;

public static class PawnHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LookupCache LookupCache(this Pawn pawn)
    {
        return XylXenos.LookupCache.Tracker.Get(pawn);
    }
}
