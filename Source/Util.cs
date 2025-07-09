using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public readonly struct ProfileBlock : IDisposable
    {
        public const bool GlobalEnabled = true;
        public const bool InstrumentTickManager = false;
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
    }

    public static class Util
    {
        public static IEnumerable<T> GenesOfType<T>(this Pawn pawn) where T : class
        {
            return pawn.genes?.GenesListForReading.OfType<T>() ?? Enumerable.Empty<T>();
        }

        public static IEnumerable<T> AnythingOfType<T>(this Pawn pawn) where T : class
        {
            if (pawn.genes != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    if (!gene.Active)
                        continue;
                    if (gene is T outGene)
                        yield return outGene;
                    if (gene.def.modExtensions != null)
                    {
                        foreach (var ext in gene.def.modExtensions)
                        {
                            if (ext is T outExt)
                                yield return outExt;
                        }
                    }
                }
            }

            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is T outHediff)
                    yield return outHediff;
                if (hediff.def.modExtensions != null)
                {
                    foreach (var ext in hediff.def.modExtensions)
                    {
                        if (ext is T outExt)
                            yield return outExt;
                    }
                }
            }
        }

        public static T FirstGeneOfType<T>(this Pawn pawn) where T : class
        {
            return pawn.genes?.GenesListForReading.OfType<T>().FirstOrDefault();
        }

        public static T FirstGeneOfType<T>(this Pawn pawn, Func<T, bool> predicate) where T : class
        {
            return pawn.genes?.GenesListForReading.OfType<T>().FirstOrDefault(predicate);
        }

        public static bool HasGeneOfType<T>(this Pawn pawn) where T : class
        {
            return pawn.genes != null && pawn.genes.GenesListForReading.OfType<T>().Any();
        }

        public static bool HasGeneOfType<T>(this Pawn pawn, Func<T, bool> predicate) where T : class
        {
            return pawn.genes != null && pawn.genes.GenesListForReading.OfType<T>().Any(predicate);
        }

        public static IEnumerable<T> GeneDefExtensionsOfType<T>(this Pawn pawn) where T : DefModExtension
        {
            if (pawn.genes == null)
                yield break;

            foreach (T extension in pawn.genes.GenesListForReading.Select(gene => gene.def.GetModExtension<T>()).Where(extension => extension != null))
                yield return extension;
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