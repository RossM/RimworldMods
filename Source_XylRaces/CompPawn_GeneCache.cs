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
        private readonly Dictionary<Type, object> geneCache = new();

        public IEnumerable<T> GetGenesOfType<T>()
        {
            if (geneCache.TryGetValue(typeof(T), out object value)) 
                return (List<T>)value;

            value = ((Pawn)parent).genes?.GenesListForReading.OfType<T>().ToList() ?? [];
            geneCache.Add(typeof(T), value);
            return (List<T>)value;
        }

        public void Notify_GenesChanged()
        {
            geneCache.Clear();
        }
    }
}
