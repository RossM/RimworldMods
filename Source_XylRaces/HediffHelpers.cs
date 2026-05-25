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
        return pawn.LookupCache().GetHediffsOfType<T>() ??
               pawn.health.hediffSet.hediffs.OfType<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<HediffWithComps> HediffsWithComp<T>(this Pawn pawn) where T : class
    {
        return pawn.LookupCache().GetHediffsWithComp<T>() ??
               pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(h => h.comps.OfType<T>().Any());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Hediff> HediffsWithDef(this Pawn pawn, HediffDef def)
    {
        return pawn.LookupCache().GetHediffsWithDef(def) ??
               pawn.health.hediffSet.hediffs.Where(h => h.def == def);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<Hediff> HediffsWithModExtension<T>(this Pawn pawn) where T : class
    {
        return pawn.LookupCache().GetHediffsWithModExtension<T>() ??
               pawn.health.hediffSet.hediffs.Where(h => h.def.modExtensions?.OfType<T>().Any() == true);
    }

    public static Hediff GetLactationHediff(HediffSet hediffSet)
    {
        return hediffSet.pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
    }
}
