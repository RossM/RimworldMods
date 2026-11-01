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

    protected override bool CanFireNowSub(IncidentParms parms)
    {
        // There must be at least one cultist for more cultists to join
        if (DefExt?.hediff != null && !Faction.OfPlayer.AllAlivePawns.Any(pawn => pawn.health.hediffSet.HasHediff(DefExt.hediff)))
            return false;

        return base.CanFireNowSub(parms);
    }

    public override Pawn GeneratePawn(Map? map)
    {
        Gender? gender = null;
        if (def.pawnFixedGender != Gender.None)
            gender = def.pawnFixedGender;

        Ideo? ideo = null;
        if (ModsConfig.IdeologyActive)
            ideo = IncidentHelpers.GetRandomIdeoNotInColony();

        XenotypeDef? xenotype = IncidentHelpers.GetRandomXenotypeNotInColony();

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
}
