namespace XylXenos;

[UsedFromXml]
public class IncidentWorker_CultistJoin : IncidentWorker_WandererJoin
{
    public override Pawn GeneratePawn(Map map)
    {
        Gender? gender = null;
        if (def.pawnFixedGender != Gender.None)
            gender = def.pawnFixedGender;

        Ideo ideo = null;
        if (ModsConfig.IdeologyActive)
            ideo = GetRandomIdeo();

        XenotypeDef xenotype = GetRandomXenotype();

        var pawnGenerationRequest = new PawnGenerationRequest(kind: def.pawnKind,
            faction: Faction.OfPlayer,
            context: PawnGenerationContext.NonPlayer,
            tile: map?.Tile,
            forceGenerateNewPawn: true,
            mustBeCapableOfViolence: def.pawnMustBeCapableOfViolence,
            colonistRelationChanceFactor: 1f,
            fixedGender: gender,
            fixedIdeo: ideo,
            forcedXenotype: xenotype);

        Pawn pawn = PawnGenerator.GeneratePawn(pawnGenerationRequest);

        var hediff = HediffMaker.MakeHediff(DefOf.XylCultistSong, pawn, pawn.health.hediffSet.GetBodyPartRecord(DefOf.Brain));
        pawn.health.AddHediff(hediff);

        return pawn;
    }

    private static XenotypeDef GetRandomXenotype()
    {
        HashSet<XenotypeDef> playerXenotypes = Faction.OfPlayer.AllPawns.Where(pawn => pawn.genes != null)
            .Select(pawn => pawn.genes.Xenotype).ToHashSet();
        XenotypeDef xenotype = null;
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

            if (!weights.Keys.Where(xenotypeDef => !playerXenotypes.Contains(xenotypeDef))
                    .TryRandomElementByWeight(xenotypeDef => weights[xenotypeDef], out xenotype))
            {
                weights.Keys.TryRandomElementByWeight(xenotypeDef => weights[xenotypeDef], out xenotype);
            }
        }

        return xenotype;
    }

    private static Ideo GetRandomIdeo()
    {
        if (!Find.IdeoManager.IdeosListForReading.Where(i => !Faction.OfPlayer.ideos.Has(i))
                .TryRandomElementByWeight(x => IdeoUtility.IdeoChangeToWeight(null, x), out Ideo ideo))
        {
            Find.IdeoManager.IdeosListForReading.Where(i => !Faction.OfPlayer.ideos.IsPrimary(i))
                .TryRandomElementByWeight(x => IdeoUtility.IdeoChangeToWeight(null, x), out ideo);
        }

        return ideo;
    }
}
