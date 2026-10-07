namespace Xylib;

[UsedFromXml]
public class StatWorker_ManhunterChance : StatWorker
{
    public override bool ShouldShowFor(StatRequest req)
    {
        Pawn? pawn = req.Pawn ?? req.Thing as Pawn;
        return pawn?.AnimalOrWildMan() is true && base.ShouldShowFor(req);
    }
}
