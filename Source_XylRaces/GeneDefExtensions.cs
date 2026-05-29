namespace XylXenos;

public static class GeneDefExtensions
{
    extension(GeneDef gene)
    {
        [CanBeNull]
        public DefModExtension_Gene DefExt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!defExtCache.TryGetValue(gene.index, out DefModExtension_Gene defExt))
                {
                    defExt = gene.GetModExtension<DefModExtension_Gene>();
                    defExtCache.Add(gene.index, defExt);
                }

                return defExt;
            }
        }
    }

    public static readonly Dictionary<int, DefModExtension_Gene> defExtCache = new();
}
