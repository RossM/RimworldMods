namespace XylXenos;

[UsedFromXml]
public class DefModExtension_IncidentWorker_CultistJoin : DefModExtension
{
    public HediffDef? hediff;
}

[UsedFromXml]
public class IncidentWorker_CultistJoin : IncidentWorker_WandererJoin
{
    public DefModExtension_IncidentWorker_CultistJoin? DefExt => def.GetModExtension<DefModExtension_IncidentWorker_CultistJoin>();

    public override Pawn GeneratePawn(Map? map)
    {
        Gender? gender = null;
        if (def.pawnFixedGender != Gender.None)
            gender = def.pawnFixedGender;

        Ideo? ideo = null;
        if (ModsConfig.IdeologyActive)
            ideo = GetRandomIdeo();

        XenotypeDef? xenotype = XenotypeHelpers.GetRandomXenotypeNotInColony();

        var pawnGenerationRequest = new PawnGenerationRequest(
            kind: def.pawnKind ?? PawnKindDefOf.Colonist,
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

        if (DefExt?.hediff != null)
        {
            var hediff = HediffMaker.MakeHediff(DefExt.hediff, pawn);
            pawn.health.AddHediff(hediff);
        }

        return pawn;
    }

    private static Ideo? GetRandomIdeo()
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
