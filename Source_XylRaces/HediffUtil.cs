using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore;

public static class HediffUtil
{
    public static IEnumerable<T> HediffsOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.GetComp<CompPawn_GeneCache>()?.GetHediffsOfType<T>() ??
               pawn.health.hediffSet.hediffs.OfType<T>();
    }

    public static IEnumerable<HediffWithComps> HediffsWithComp<T>(this Pawn pawn) where T : class
    {
        return pawn.GetComp<CompPawn_GeneCache>()?.GetHediffsWithComp<T>() ??
               pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(h => h.comps.OfType<T>().Any());
    }

    public static HediffWithComps GetFirstHediffWithComp<T>(this Pawn pawn) where T : class
    {
        return pawn.HediffsWithComp<T>().FirstOrDefault();
    }

    public static HediffWithComps GetFirstHediffWithComp<T>(this HediffSet hediffSet) where T : class
    {
        return hediffSet.pawn.GetFirstHediffWithComp<T>();
    }

    public static IEnumerable<Hediff> GetHediffsWithDef(this Pawn pawn, HediffDef def)
    {
        return pawn.GetComp<CompPawn_GeneCache>()?.GetHediffsWithDef(def) ??
               pawn.health.hediffSet.hediffs.Where(h => h.def == def);
    }

    public static Hediff GetFirstHediffWithDef(this Pawn pawn, HediffDef def)
    {
        return pawn.GetHediffsWithDef(def).FirstOrDefault();
    }

    public static bool HasHediffWithComp<T>(this Pawn pawn) where T : class
    {
        return pawn.HediffsWithComp<T>().Any();
    }

    public static bool HasHediffWithComp<T>(this HediffSet hediffSet) where T : class
    {
        return hediffSet.pawn.HasHediffWithComp<T>();
    }

    public static IEnumerable<Hediff> HediffsWithModExtension<T>(this Pawn pawn) where T : class
    {
        return pawn.GetComp<CompPawn_GeneCache>()?.GetHediffsWithModExtension<T>() ??
               pawn.health.hediffSet.hediffs.Where(h => h.def.modExtensions.OfType<T>().Any());
    }
}