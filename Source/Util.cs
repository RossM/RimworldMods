using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public static class Util
    {
        public static IEnumerable<T> GenesOfType<T>(this Pawn pawn) where T : class
        {
            return pawn.genes?.GenesListForReading.OfType<T>() ?? Enumerable.Empty<T>();
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