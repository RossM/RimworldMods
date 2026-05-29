using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class GeneExtensions
{
    extension(Gene gene)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CanBeNull]
        public DefExt DefExt()
        {
            return gene.def.DefExt();
        }
    }
}