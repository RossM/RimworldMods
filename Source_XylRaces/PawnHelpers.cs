using System.Runtime.CompilerServices;
using Verse;

namespace XylXenos;

public static class PawnHelpers
{
    extension(Pawn pawn)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LookupCache LookupCache()
        {
            return XylXenos.LookupCache.Tracker.Get(pawn);
        }
    }
}
