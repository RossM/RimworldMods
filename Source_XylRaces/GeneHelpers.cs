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

    extension(Pawn pawn)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> GenesOfDef(GeneDef def)
        {
            if (pawn.genes == null)
                return [];

            return pawn.LookupCache().GetGenesWithDef(def);
        }

        // This is faster than pawn.genes.HasActiveGene(def) because it caches
        // the gene lookup.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGene(GeneDef def)
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
        public IEnumerable<T> GenesOfType<T>() where T : class
        {
            if (pawn.genes == null)
                return [];

            return pawn.LookupCache().GetGenesOfType<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<T> ActiveGenesOfType<T>() where T : class
        {
            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    yield return g;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstActiveGeneOfType<T>() where T : class
        {
            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    return g;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGeneOfType<T>() where T : class
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
        public bool HasActiveGeneOfType<T>(Func<T, bool> predicate) where T : class
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

        public IEnumerable<DefExt> ActiveDefExts()
        {
            if (pawn.genes == null)
                return [];
            return pawn.genes.GenesListForReading.Where(gene => gene.Active).Select(gene => gene.DefExt()).Where(defExt => defExt != null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CanBeNull]
        public GeneSet GeneSet()
        {
            if (pawn.genes == null)
                return null;

            return XylXenos.GeneSet.Tracker.Get(pawn);
        }
    }

    extension(GeneDef gene)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CanBeNull]
        public DefExt DefExt()
        {
            if (!defExtCache.TryGetValue(gene.index, out DefExt defExt))
            {
                defExt = gene.GetModExtension<DefExt>();
                defExtCache.Add(gene.index, defExt);
            }

            return defExt;
        }
    }

    extension(Gene gene)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CanBeNull]
        public DefExt DefExt()
        {
            return gene.def.DefExt();
        }
    }
}
