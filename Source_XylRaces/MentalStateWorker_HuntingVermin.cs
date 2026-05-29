namespace XylXenos;

[UsedFromXml]
public class MentalStateWorker_HuntingVermin : MentalStateWorker
{
    public override bool StateCanOccur(Pawn pawn)
    {
        if (!base.StateCanOccur(pawn))
        {
            return false;
        }

        return MentalState_HuntingVermin.FindPawnToKill(pawn) != null;
    }
}