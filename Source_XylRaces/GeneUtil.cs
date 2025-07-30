using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylRacesCore;

public static class GeneUtil
{
    public static IEnumerable<Gene> GenesOfDef(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes == null)
            return Enumerable.Empty<Gene>();

        return pawn.GetComp<CompPawn_GeneCache>()?.GetGenesWithDef(def) ??
               pawn.genes.GenesListForReading.Where(g => g.def == def);
    }

    // This is faster than pawn.genes.HasActiveGene(def) because it caches
    // the gene lookup.
    public static bool HasActiveGene(this Pawn pawn, GeneDef def)
    {
        return pawn.genes != null && def != null && pawn.GenesOfDef(def).Any(g => g.Active);
    }

    public static IEnumerable<T> GenesOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<T>();

        return pawn.GetComp<CompPawn_GeneCache>()?.GetGenesOfType<T>() ??
               pawn.genes.GenesListForReading.OfType<T>();
    }

    public static IEnumerable<T> ActiveGenesOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.GenesOfType<T>().Where(g => ((Gene)(object)g).Active);
    }

    public static T FirstActiveGeneOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.GenesOfType<T>().FirstOrDefault(g => ((Gene)(object)g).Active);
    }

    public static bool HasActiveGeneOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.genes != null && pawn.GenesOfType<T>().Any(g => ((Gene)(object)g).Active);
    }

    public static bool HasActiveGeneOfType<T>(this Pawn pawn, Func<T, bool> predicate) where T : class
    {
        return pawn.genes != null && pawn.GenesOfType<T>().Any(g => ((Gene)(object)g).Active && predicate(g));
    }

    public static IEnumerable<Gene> GenesWithModExtension<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<Gene>();

        return pawn.GetComp<CompPawn_GeneCache>()?.GetGenesWithModExtension<T>() ??
               pawn.genes.GenesListForReading.Where(g => g.def.modExtensions.OfType<T>().Any());
    }

    public static IEnumerable<T> ActiveGeneDefExtensionsOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<T>();

        return pawn.GenesWithModExtension<T>().Where(g => g.Active).SelectMany(g => g.def.modExtensions.OfType<T>());
    }
}