namespace XylXenos;

public static class GeneDefExtensions
{
    extension(GeneDef gene)
    {
        [CanBeNull]
        public DefExt DefExt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!defExtCache.TryGetValue(gene.index, out DefExt defExt))
                {
                    defExt = gene.GetModExtension<DefExt>();
                    defExtCache.Add(gene.index, defExt);
                }

                return defExt;
            }
        }
    }

    public static readonly Dictionary<int, DefExt> defExtCache = new();
}
