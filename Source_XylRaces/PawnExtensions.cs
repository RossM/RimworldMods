using RimWorld.Planet;

namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public Hediff LactationHediff => pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();

    }
}
