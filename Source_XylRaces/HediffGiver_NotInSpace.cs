namespace XylXenos;

[UsedFromXml]
public class HediffGiver_RandomNotInSpace : HediffGiver_Random
{
    public override float ChanceFactor(Pawn pawn)
    {
        if (pawn.Spawned && pawn.MapHeld.Tile.Valid && pawn.MapHeld.Tile.LayerDef.isSpace)
            return 0f;
        return base.ChanceFactor(pawn);
    }
}
