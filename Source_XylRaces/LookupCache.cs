using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace XylXenos
{
    public class LookupCache(Pawn pawn) : INotificationListener
    {
        public static readonly PawnTracker<LookupCache> Tracker = new(Make);
        public Pawn pawn = pawn;

        private readonly Dictionary<Type, IList> genesByType = new();
        private readonly Dictionary<GeneDef, List<Gene>> genesByDef = new();
        private readonly Dictionary<Type, List<Gene>> genesByModExt = new();

        private readonly Dictionary<Type, IList> hediffsByType = new();
        private readonly Dictionary<HediffDef, List<Hediff>> hediffsByDef = new();
        private readonly Dictionary<Type, List<Hediff>> hediffsByModExt = new();
        private readonly Dictionary<Type, List<HediffWithComps>> hediffsByComp = new();

        private static LookupCache Make(Pawn pawn)
        {
            var cache = new LookupCache(pawn);
            cache.RegisterWith(NotificationManager.Instance);
            return cache;
        }

        public IEnumerable<T> GetGenesOfType<T>()
        {
            if (genesByType.TryGetValue(typeof(T), out IList value))
                return (List<T>)value;

            value = pawn.genes?.GenesListForReading.OfType<T>().ToList() ?? [];
            genesByType.Add(typeof(T), value);
            return (List<T>)value;
        }

        public List<Gene> GetGenesWithDef(GeneDef def)
        {
            if (genesByDef.TryGetValue(def, out List<Gene> value))
                return value;

            value = pawn.genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList() ?? [];
            genesByDef.Add(def, value);
            return value;
        }

        public IEnumerable<Gene> GetGenesWithModExtension<T>() where T : class
        {
            if (genesByModExt.TryGetValue(typeof(T), out List<Gene> value))
                return value;

            value = pawn.genes?.GenesListForReading.Where(g => g.def.modExtensions?.OfType<T>().Any() == true).ToList() ?? [];
            genesByModExt.Add(typeof(T), value);
            return value;
        }

        public void Notify_GenesChanged()
        {
            genesByDef.Clear();
            genesByModExt.Clear();
            genesByType.Clear();
        }

        public IEnumerable<T> GetHediffsOfType<T>()
        {
            if (hediffsByType.TryGetValue(typeof(T), out IList value))
                return (List<T>)value;

            value = pawn.health.hediffSet.hediffs.OfType<T>().ToList();
            hediffsByType.Add(typeof(T), value);
            return (List<T>)value;
        }

        public List<Hediff> GetHediffsWithDef(HediffDef def)
        {
            if (hediffsByDef.TryGetValue(def, out List<Hediff> value))
                return value;

            value = pawn.health.hediffSet.hediffs.Where(g => g.def == def).ToList();
            hediffsByDef.Add(def, value);
            return value;
        }

        public IEnumerable<Hediff> GetHediffsWithModExtension<T>() where T : class
        {
            if (hediffsByModExt.TryGetValue(typeof(T), out List<Hediff> value))
                return value;

            value = pawn.health.hediffSet.hediffs.Where(g => g.def.modExtensions?.OfType<T>().Any() == true).ToList();
            hediffsByModExt.Add(typeof(T), value);
            return value;
        }

        public IEnumerable<HediffWithComps> GetHediffsWithComp<T>() where T : class
        {
            if (hediffsByComp.TryGetValue(typeof(T), out List<HediffWithComps> value))
                return value;

            value = pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(g => g.comps?.OfType<T>().Any() == true)
                .ToList();
            hediffsByComp.Add(typeof(T), value);
            return value;
        }

        public void Notify_HediffsChanged()
        {
            hediffsByComp.Clear();
            hediffsByDef.Clear();
            hediffsByModExt.Clear();
            hediffsByType.Clear();
        }

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostGenesChanged, pawn, Notify_GenesChanged);
            manager.Register(NotificationEvent.PostHediffsChanged, pawn, Notify_HediffsChanged);
        }
    }
}
