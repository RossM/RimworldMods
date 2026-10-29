namespace XylXenos;

public static class IncidentHelpers
{
    public static XenotypeDef? GetRandomXenotypeNotInColony()
    {
        HashSet<XenotypeDef> playerXenotypes =
        [
            .. Faction.OfPlayer.AllAlivePawns.Where(pawn => pawn.genes is { CustomXenotype: null })
                .Select(pawn => pawn.genes!.Xenotype),
        ];
        Dictionary<XenotypeDef, float> weights = new();
        foreach (var faction in Find.FactionManager.AllFactionsVisible)
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
            weights.Keys.TryRandomElementByWeight(xenotypeDef => weights[xenotypeDef], out xenotype);

        return xenotype;
    }

    public static Ideo? GetRandomIdeoNotInColony()
    {
        DebugAssert.NotNull(Find.IdeoManager);

        var playerIdeos = Faction.OfPlayer.ideos;
        DebugAssert.NotNull(playerIdeos);

        if (!Find.IdeoManager.IdeosListForReading.Where(i => !playerIdeos.Has(i))
                .TryRandomElementByWeight(x => IdeoUtility.IdeoChangeToWeight(null, x), out Ideo? ideo))
            Find.IdeoManager.IdeosListForReading.Where(i => !playerIdeos.IsPrimary(i))
                .TryRandomElementByWeight(x => IdeoUtility.IdeoChangeToWeight(null, x), out ideo);

        return ideo;
    }
}
