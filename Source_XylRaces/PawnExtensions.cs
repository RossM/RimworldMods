using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LookupCache LookupCache()
        {
            return XylXenos.LookupCache.Tracker.Get(pawn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> GenesOfDef(GeneDef def)
        {
            if (pawn.genes == null)
                return [];

            return pawn.LookupCache().GetGenesWithDef(def);
        }

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

        public int GetGeneticPsylinkLevelFor(AbilityDef def)
        {
            if (pawn.genes != null && pawn.genes.GenesListForReading.Any(gene =>
                    gene.Active && gene.DefExt()?.hasPsycast == true && gene.def.abilities?.Any(abilityDef => abilityDef == def) == true))
            {
                return def.level;
            }

            return 0;
        }

        public bool HasActivePsycastGene()
        {
            return pawn.GeneSet()?.hasPsycast == true;
        }

        public bool NeedsPsyfocus()
        {
            // HasPsylink is patched to respect psycast genes
            if (!pawn.HasPsylink)
                return false;
            if (pawn.Suspended)
                return false;
            if (!pawn.Spawned && !pawn.IsCaravanMember())
                return false;
            return true;
        }
    }
}