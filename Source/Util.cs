using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    public static class Util
    {
        public static float GetStatValue(this Pawn pawn, string defName, float defaultValue)
        {
            StatDef statValue = DefDatabase<StatDef>.GetNamed(defName, errorOnFail: false);
            return statValue != null ? pawn.GetStatValue(statValue) : defaultValue;
        }

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

        public static Hediff GetFirstHediffWithComp<T>(this HediffSet hediffSet) where T : HediffComp
        {
            return hediffSet.hediffs.FirstOrDefault(h => h.TryGetComp<T>() != null);
        }

        public static bool HasHediffWithComp<T>(this HediffSet hediffSet) where T : HediffComp
        {
            return hediffSet.hediffs.Any(h => h.TryGetComp<T>() != null);
        }
    }
}