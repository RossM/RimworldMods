using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class HediffGiver_RandomExt : HediffGiver
    {
        public float mtbDays;
        public bool sendLetter = true;

        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            float num = mtbDays;
            float num2 = ChanceFactor(pawn);
            if (num2 != 0f && Rand.MTBEventOccurs(num / num2, 60000f, 60f) && TryApply(pawn))
            {
                if (sendLetter)
                    SendLetter(pawn, cause);
            }
        }
    }
}
