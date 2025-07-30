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
        private readonly Dictionary<Type, object> typeCache = new();
        private readonly Dictionary<GeneDef, List<Gene>> defCache = new();
        private readonly Dictionary<Type, List<Gene>> modCache = new();

        public IEnumerable<T> GetGenesOfType<T>()
        {
            if (typeCache.TryGetValue(typeof(T), out object value)) 
                return (List<T>)value;

            value = ((Pawn)parent).genes?.GenesListForReading.OfType<T>().ToList() ?? [];
            typeCache.Add(typeof(T), value);
            return (List<T>)value;
        }

        public List<Gene> GetGenes(GeneDef def)
        {
            if (defCache.TryGetValue(def, out List<Gene> value))
                return value;

            value = ((Pawn)parent).genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList();
            defCache.Add(def, value);
            return value;
        }

        public IEnumerable<Gene> GetGenesWithModExtension<T>() where T : class
        {
            if (modCache.TryGetValue(typeof(T), out List<Gene> value))
                return value;

            value = ((Pawn)parent).genes?.GenesListForReading.Where(g => g.def.modExtensions.OfType<T>().Any()).ToList() ?? [];
            modCache.Add(typeof(T), value);
            return value;
        }

        public void Notify_GenesChanged()
        {
            typeCache.Clear();
            defCache.Clear();
            modCache.Clear();
        }
    }
}
