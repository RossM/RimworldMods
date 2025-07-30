using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class CompProperties_PawnGeneCache : CompProperties
    {
        public CompProperties_PawnGeneCache()
        {
            compClass = typeof(CompPawn_GeneCache);
        }
    }

    public class CompPawn_GeneCache : ThingComp
    {
        [Unsaved]
        private readonly Dictionary<Type, object> genesByType = new();
        [Unsaved]
        private readonly Dictionary<GeneDef, List<Gene>> genesByDef = new();
        [Unsaved]
        private readonly Dictionary<Type, List<Gene>> genesByModExt = new();

        [Unsaved] 
        private readonly Dictionary<Type, object> hediffsByType = new();
        [Unsaved] 
        private readonly Dictionary<HediffDef, List<Hediff>> hediffsByDef = new();
        [Unsaved]
        private readonly Dictionary<Type, List<Hediff>> hediffsByModExt = new();
        [Unsaved]
        private readonly Dictionary<Type, List<HediffWithComps>> hediffsByComp = new();

        public IEnumerable<T> GetGenesOfType<T>()
        {
            if (genesByType.TryGetValue(typeof(T), out object value)) 
                return (List<T>)value;

            value = ((Pawn)parent).genes?.GenesListForReading.OfType<T>().ToList() ?? [];
            genesByType.Add(typeof(T), value);
            return (List<T>)value;
        }

        public List<Gene> GetGenesWithDef(GeneDef def)
        {
            if (genesByDef.TryGetValue(def, out List<Gene> value))
                return value;

            value = ((Pawn)parent).genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList();
            genesByDef.Add(def, value);
            return value;
        }

        public IEnumerable<Gene> GetGenesWithModExtension<T>() where T : class
        {
            if (genesByModExt.TryGetValue(typeof(T), out List<Gene> value))
                return value;

            value = ((Pawn)parent).genes?.GenesListForReading.Where(g => g.def.modExtensions.OfType<T>().Any()).ToList() ?? [];
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
            if (hediffsByType.TryGetValue(typeof(T), out object value))
                return (List<T>)value;

            value = ((Pawn)parent).health.hediffSet.hediffs.OfType<T>().ToList() ?? [];
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

            value = ((Pawn)parent).health.hediffSet.hediffs.Where(g => g.def.modExtensions.OfType<T>().Any()).ToList();
            hediffsByModExt.Add(typeof(T), value);
            return value;
        }

        public IEnumerable<HediffWithComps> GetHediffsWithComp<T>() where T : class
        {
            if (hediffsByComp.TryGetValue(typeof(T), out List<HediffWithComps> value))
                return value;

            value = ((Pawn)parent).health.hediffSet.hediffs.OfType<HediffWithComps>().Where(g => g.comps.OfType<T>().Any()).ToList();
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
    }
}
