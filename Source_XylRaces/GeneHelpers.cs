using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class GeneHelpers
{
    public static readonly Dictionary<int, DefExt> defExtCache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerable<Gene> GenesOfDef(this Pawn pawn, GeneDef def)
    {
        if (pawn.genes == null)
            return [];

        return pawn.LookupCache().GetGenesWithDef(def);
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
            return [];

        return pawn.LookupCache().GetGenesOfType<T>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // ReSharper disable once UnusedMember.Global
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

    public static IEnumerable<DefExt> ActiveDefExts(this Pawn pawn)
    {
        if (pawn.genes == null)
            return [];
        return pawn.genes.GenesListForReading.Where(gene => gene.Active).Select(gene => gene.DefExt()).Where(defExt => defExt != null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CanBeNull]
    public static GeneSet GeneSet(this Pawn pawn)
    {
        if (pawn.genes == null)
            return null;

        return XylXenos.GeneSet.Tracker.Get(pawn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CanBeNull]
    public static DefExt DefExt(this GeneDef gene)
    {
        if (!defExtCache.TryGetValue(gene.index, out DefExt defExt))
        {
            defExt = gene.GetModExtension<DefExt>();
            defExtCache.Add(gene.index, defExt);
        }

        return defExt;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [CanBeNull]
    public static DefExt DefExt(this Gene gene)
    {
        return gene.def.DefExt();
    }
}
