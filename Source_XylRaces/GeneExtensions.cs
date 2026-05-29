using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Verse;
using XylXenos.Genes;

namespace XylXenos;

public static class GeneExtensions
{
    extension(Gene gene)
    {
        [CanBeNull]
        public DefExt DefExt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => gene.def.DefExt;
        }
    }
}