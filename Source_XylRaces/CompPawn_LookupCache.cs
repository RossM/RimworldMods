using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class CompProperties_PawnLookupCache : CompProperties
    {
        public CompProperties_PawnLookupCache()
        {
            compClass = typeof(CompPawn_LookupCache);
        }
    }

    public class CompPawn_LookupCache : ThingComp, INotificationListener
    {
        [Unsaved] private readonly Dictionary<Type, IList> genesByType = new();
        [Unsaved] private readonly Dictionary<GeneDef, List<Gene>> genesByDef = new();
        [Unsaved] private readonly Dictionary<Type, List<Gene>> genesByModExt = new();

        [Unsaved] private readonly Dictionary<Type, IList> hediffsByType = new();
        [Unsaved] private readonly Dictionary<HediffDef, List<Hediff>> hediffsByDef = new();
        [Unsaved] private readonly Dictionary<Type, List<Hediff>> hediffsByModExt = new();
        [Unsaved] private readonly Dictionary<Type, List<HediffWithComps>> hediffsByComp = new();

        public IEnumerable<T> GetGenesOfType<T>()
        {
            if (genesByType.TryGetValue(typeof(T), out IList value))
                return (List<T>)value;

            value = ((Pawn)parent).genes?.GenesListForReading.OfType<T>().ToList() ?? [];
            genesByType.Add(typeof(T), value);
            return (List<T>)value;
        }

        public List<Gene> GetGenesWithDef(GeneDef def)
        {
            if (genesByDef.TryGetValue(def, out List<Gene> value))
                return value;

            value = ((Pawn)parent).genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList() ?? [];
            genesByDef.Add(def, value);
            return value;
        }

        public IEnumerable<Gene> GetGenesWithModExtension<T>() where T : class
        {
            if (genesByModExt.TryGetValue(typeof(T), out List<Gene> value))
                return value;

            value = ((Pawn)parent).genes?.GenesListForReading.Where(g => g.def.modExtensions?.OfType<T>().Any() == true).ToList() ?? [];
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

            value = ((Pawn)parent).health.hediffSet.hediffs.OfType<T>().ToList();
            hediffsByType.Add(typeof(T), value);
            return (List<T>)value;
        }

        public List<Hediff> GetHediffsWithDef(HediffDef def)
        {
            if (hediffsByDef.TryGetValue(def, out List<Hediff> value))
                return value;

            value = ((Pawn)parent).health.hediffSet.hediffs.Where(g => g.def == def).ToList();
            hediffsByDef.Add(def, value);
            return value;
        }

        public IEnumerable<Hediff> GetHediffsWithModExtension<T>() where T : class
        {
            if (hediffsByModExt.TryGetValue(typeof(T), out List<Hediff> value))
                return value;

            value = ((Pawn)parent).health.hediffSet.hediffs.Where(g => g.def.modExtensions?.OfType<T>().Any() == true).ToList();
            hediffsByModExt.Add(typeof(T), value);
            return value;
        }

        public IEnumerable<HediffWithComps> GetHediffsWithComp<T>() where T : class
        {
            if (hediffsByComp.TryGetValue(typeof(T), out List<HediffWithComps> value))
                return value;

            value = ((Pawn)parent).health.hediffSet.hediffs.OfType<HediffWithComps>().Where(g => g.comps?.OfType<T>().Any() == true)
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
            manager.Register(NotificationEvent.GenesChanged, parent, Notify_GenesChanged);
            manager.Register(NotificationEvent.HediffsChanged, parent, Notify_HediffsChanged);
        }
    }
}
