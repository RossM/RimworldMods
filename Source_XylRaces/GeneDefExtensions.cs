using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class GeneDefExtensions
{
    extension(GeneDef gene)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CanBeNull]
        public DefExt DefExt()
        {
            if (!defExtCache.TryGetValue(gene.index, out DefExt defExt))
            {
                defExt = gene.GetModExtension<DefExt>();
                defExtCache.Add(gene.index, defExt);
            }

            return defExt;
        }
    }

    public static readonly Dictionary<int, DefExt> defExtCache = new();
}