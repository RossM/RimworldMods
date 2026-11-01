namespace XylXenos;

[UsedFromXml]
public class HediffGiver_RandomNotInSpace : HediffGiver_Random
{
    [MustTranslate] public string? message;

    public override float ChanceFactor(Pawn pawn)
    {
        if (pawn is { Spawned: true, MapHeld.Tile.Valid: true } && pawn.MapHeld.Tile.LayerDef.isSpace)
            return 0f;
        return base.ChanceFactor(pawn);
    }

    public override void OnIntervalPassed(Pawn pawn, Hediff? cause)
    {
        float mtb = mtbDays;
        float factor = ChanceFactor(pawn);
        if (factor != 0f && Rand.MTBEventOccurs(mtb / factor, 60000f, 60f) && TryApply(pawn))
            if (message != null)
                Messages.Message(message.Formatted(pawn.LabelShortCap, hediff.LabelCap, pawn.Named("PAWN")).CapitalizeFirst(),
                    MessageTypeDefOf.NegativeEvent);
    }
}
