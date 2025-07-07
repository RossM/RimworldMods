using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public readonly struct ProfileBlock : IDisposable
    {
        public const bool GlobalEnabled = true;
        public const bool InstrumentTickManager = false;
        private readonly bool _enabled;
        private static readonly Dictionary<MethodBase, string> fastLabels = new();

        public ProfileBlock(string label, bool enabled = GlobalEnabled)
        {
            _enabled = enabled;
            if (!_enabled) 
                return;
            DeepProfiler.Start(label);
        }

        public ProfileBlock(MethodBase method = null, [System.Runtime.CompilerServices.CallerMemberName] string methodName = null, bool enabled = GlobalEnabled)
        {
            _enabled = enabled;
            if (!_enabled) 
                return;
            string label = methodName;
            if (label == null && method == null)
                label = "<Unknown>";
            else if (label == null && !fastLabels.TryGetValue(method, out label))
            {
                label = method.DeclaringType == null
                    ? "<Unknown>." + method.Name
                    : method.DeclaringType.Assembly.GetName().Name + "." + method.DeclaringType.Name + "." +
                      method.Name;
                fastLabels.Add(method, label);
            }

            DeepProfiler.Start(label);
        }

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
    }
}