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

            foreach (T hediff in pawn.HediffsOfType<T>())
                yield return hediff;
            foreach (T hediffDefExt in pawn.HediffsWithModExtension<T>().SelectMany(h => h.def.modExtensions.OfType<T>()))
                yield return hediffDefExt;
            foreach (T hediffComp in pawn.HediffsWithComp<T>().SelectMany(h => h.comps.OfType<T>()))
                yield return hediffComp;
        }

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