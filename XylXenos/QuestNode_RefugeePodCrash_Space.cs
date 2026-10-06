using RimWorld.Planet;
using RimWorld.QuestGen;

namespace XylXenos;

[UsedFromXml]
public class QuestNode_RefugeePodCrash_Space : QuestNode_Root_RefugeePodCrash
{
    public SlateRef<PawnKindDef> kindDef;

    public bool forceNoGear = false;

    protected override bool CanBeSpace => true;

    protected override bool TestRunInt(Slate slate)
    {
        // IncidentWorker_GiveQuest_Map preserves this target during generation. The vanilla
        // test incorrectly requires a surface home map when CanBeSpace is true.
        Map? map = slate.Get<Map>("map");
        return map is not null && map.IsPlayerHome && kindDef.GetValue(slate) is not null;
    }

    public override Pawn GeneratePawn_NewTemp(Map map)
    {
        var request = new PawnGenerationRequest(
            kind: kindDef.GetValue(QuestGen.slate),
            faction: null,
            context: PawnGenerationContext.NonPlayer,
            tile: map.Tile,
            forceGenerateNewPawn: true,
            forceNoGear: forceNoGear,
            colonistRelationChanceFactor: 20f);

        // Match the standard refugee pod's injuries and retry limit.
        Pawn? pawn = null;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (pawn is not null)
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);

            pawn = PawnGenerator.GeneratePawn(request);
            HealthUtility.DamageUntilDowned(pawn);
            if (pawn.Downed)
                break;
        }

        DebugAssert.NotNull(pawn);
        pawn.guest.Recruitable = true;

        return pawn;
    }
}
