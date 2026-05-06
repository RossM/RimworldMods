using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Verse;

namespace XylRacesCore;

public static class GeneUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Gene> GenesOfDef(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes == null)
            return Enumerable.Empty<Gene>();

        return pawn.GetComp<CompPawn_LookupCache>()?.GetGenesWithDef(def);
    }

    // This is faster than pawn.genes.HasActiveGene(def) because it caches
    // the gene lookup.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGene(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes == null || def == null) 
            return false;

        foreach (Gene g in pawn.GenesOfDef(def))
        {
            if (g.Active) 
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> GenesOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<T>();

        return pawn.GetComp<CompPawn_LookupCache>()?.GetGenesOfType<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ActiveGenesOfType<T>(this Pawn pawn) where T : class
    {
        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active)
                yield return g;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T FirstActiveGeneOfType<T>(this Pawn pawn) where T : class
    {
        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active) 
                return g;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGeneOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null) 
            return false;

        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active) 
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasActiveGeneOfType<T>(this Pawn pawn, Func<T, bool> predicate) where T : class
    {
        if (pawn.genes == null) 
            return false;

        foreach (T g in pawn.GenesOfType<T>())
        {
            if (((Gene)(object)g).Active && predicate(g)) 
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Gene> GenesWithModExtension<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<Gene>();

        return pawn.GetComp<CompPawn_LookupCache>()?.GetGenesWithModExtension<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<T> ActiveGeneDefExtensionsOfType<T>(this Pawn pawn) where T : class
    {
        if (pawn.genes == null)
            return Enumerable.Empty<T>();

        return pawn.GenesWithModExtension<T>().Where(g => g.Active).SelectMany(g => g.def.modExtensions.OfType<T>());
    }
}