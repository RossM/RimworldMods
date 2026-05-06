using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace XylRacesCore;

public static class HediffUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> HediffsOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.GetComp<CompPawn_LookupCache>()?.GetHediffsOfType<T>() ??
               pawn.health.hediffSet.hediffs.OfType<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<HediffWithComps> HediffsWithComp<T>(this Pawn pawn) where T : class
    {
        return pawn.GetComp<CompPawn_LookupCache>()?.GetHediffsWithComp<T>() ??
               pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(h => h.comps.OfType<T>().Any());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Hediff> HediffsWithDef(this Pawn pawn, HediffDef def)
    {
        return pawn.GetComp<CompPawn_LookupCache>()?.GetHediffsWithDef(def) ??
               pawn.health.hediffSet.hediffs.Where(h => h.def == def);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Hediff> HediffsWithModExtension<T>(this Pawn pawn) where T : class
    {
        return pawn.GetComp<CompPawn_LookupCache>()?.GetHediffsWithModExtension<T>() ??
               pawn.health.hediffSet.hediffs.Where(h => h.def.modExtensions?.OfType<T>().Any() == true);
    }
}