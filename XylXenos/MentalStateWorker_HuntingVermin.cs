namespace XylXenos;

[UsedFromXml]
public class MentalStateWorker_HuntingVermin : MentalStateWorker
{
    public override bool StateCanOccur(Pawn pawn) => base.StateCanOccur(pawn) && MentalState_HuntingVermin.FindPawnToKill(pawn) != null;
}
