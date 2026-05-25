using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace XylXenos;

public static class HediffHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> HediffsOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.LookupCache().GetHediffsOfType<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<HediffWithComps> HediffsWithComp<T>(this Pawn pawn) where T : class
    {
        return pawn.LookupCache().GetHediffsWithComp<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<Hediff> HediffsWithDef(this Pawn pawn, HediffDef def)
    {
        return pawn.LookupCache().GetHediffsWithDef(def);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<Hediff> HediffsWithModExtension<T>(this Pawn pawn) where T : class
    {
        return pawn.LookupCache().GetHediffsWithModExtension<T>();
    }

    public static Hediff GetLactationHediff(HediffSet hediffSet)
    {
        return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
    }
}
