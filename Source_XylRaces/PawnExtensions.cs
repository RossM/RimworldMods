using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public LookupCache LookupCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => LookupCache.Tracker.Get(pawn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> GenesOfDef(GeneDef def)
        {
            if (pawn.genes == null)
                return [];

            return pawn.LookupCache.GetGenesWithDef(def);
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

            return pawn.LookupCache.GetGenesOfType<T>();
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

        [CanBeNull]
        public GeneSet GeneSet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : GeneSet.Tracker.Get(pawn);
        }

        public int GetGeneticPsylinkLevelFor(AbilityDef def)
        {
            if (pawn.genes != null && pawn.genes.GenesListForReading.Any(gene =>
                    gene.Active && gene.def.DefExt?.hasPsycast == true && gene.def.abilities?.Any(abilityDef => abilityDef == def) == true))
            {
                return def.level;
            }

            return 0;
        }

        public bool HasActivePsycastGene => pawn.GeneSet?.hasPsycast == true;

        public bool NeedsPsyfocus
        {
            get
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> HediffsOfType<T>() where T : class
        {
            return pawn.LookupCache.GetHediffsOfType<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<HediffWithComps> HediffsWithComp<T>() where T : class
        {
            return pawn.LookupCache.GetHediffsWithComp<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def)
        {
            return pawn.LookupCache.GetHediffsWithDef(def);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Hediff> HediffsWithModExtension<T>() where T : class
        {
            return pawn.LookupCache.GetHediffsWithModExtension<T>();
        }

        public Hediff LactationHediff => pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();
    }
}