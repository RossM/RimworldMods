using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using LudeonTK;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public readonly struct ProfileBlock : IDisposable
    {
        public const bool GlobalEnabled = true;
        public static bool InstrumentTickManager = false;
        private readonly bool _enabled;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ProfileBlock(bool enabled = GlobalEnabled, [CallerMemberName] string methodName = null)
        {
            _enabled = enabled;
            if (!_enabled) 
                return;
            string label = methodName ?? "<Unknown>";

            DeepProfiler.Start(label);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if (!_enabled) 
                return;
            DeepProfiler.End();
        }

        [DebugAction("Toggle tick profiling"), UsedImplicitly]
        public static void ToggleTickProfiling()
        {
            InstrumentTickManager = !InstrumentTickManager;
        }
    }

    public static class Util
    {
        public static IEnumerable<T> EverythingOfType<T>(this Pawn pawn) where T : class
        {
            if (pawn.genes != null)
            {
                foreach (T gene in pawn.ActiveGenesOfType<T>())
                    yield return gene;
                foreach (T geneDefExt in pawn.ActiveGeneDefExtensionsOfType<T>())
                    yield return geneDefExt;
            }

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is T outHediff)
                    yield return outHediff;
                if (hediff.def.modExtensions != null)
                {
                    foreach (var ext in hediff.def.modExtensions.OfType<T>())
                        yield return ext;
                }
            }
        }

        public static IEnumerable<Gene> GenesOfDef(this Pawn pawn, GeneDef def)
        {
            if (pawn.genes == null)
                return Enumerable.Empty<Gene>();

            return pawn.GetComp<CompPawn_GeneCache>()?.GetGenes(def) ??
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

        public static IEnumerable<T> HediffsOfType<T>(this Pawn pawn)
        {
            return pawn.health.hediffSet.hediffs.OfType<T>();
        }
        public static Hediff GetFirstHediffWithComp<T>(this HediffSet hediffSet) where T : HediffComp
        {
            return hediffSet.hediffs.FirstOrDefault(h => h.TryGetComp<T>() != null);
        }

        public static bool HasHediffWithComp<T>(this HediffSet hediffSet) where T : HediffComp
        {
            return hediffSet.hediffs.Any(h => h.TryGetComp<T>() != null);
        }

        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> enumerable)
        {
            return enumerable ?? Enumerable.Empty<T>();
        }

        public static float GetStatBase(this ThingDef thingDef, StatDef statDef)
        {
            return thingDef.statBases.FirstOrDefault(s => s.stat == statDef)?.value ?? 0;
        }
    }
}