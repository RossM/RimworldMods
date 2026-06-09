namespace XylXenos;

public static class XenotypeHelpers
{
    public static XenotypeDef GetRandomXenotypeNotInColony()
    {
        HashSet<XenotypeDef> playerXenotypes = Faction.OfPlayer.AllPawns.Where(pawn => pawn.genes != null)
            .Select(pawn => pawn.genes.Xenotype).ToHashSet();
        Dictionary<XenotypeDef, float> weights = new();
        foreach (var faction in Find.FactionManager.AllFactionsListForReading)
        {
            var xenotypeSet = faction.def.xenotypeSet;
            if (xenotypeSet == null)
                continue;

            for (int i = 0; i < xenotypeSet.Count; i++)
            {
                var entry = xenotypeSet[i];
                weights[entry.xenotype] = weights.GetWithFallback(entry.xenotype) + entry.chance;
            }
        }

        if (!weights.Keys.Where(xenotypeDef => !playerXenotypes.Contains(xenotypeDef))
                .TryRandomElementByWeight(xenotypeDef => weights[xenotypeDef], out XenotypeDef xenotype))
        {
            weights.Keys.TryRandomElementByWeight(xenotypeDef => weights[xenotypeDef], out xenotype);
        }

        return xenotype;
    }
}