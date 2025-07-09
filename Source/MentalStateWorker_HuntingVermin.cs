using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
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

}
