namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public Hediff LactationHediff => pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();

        [CanBeNull]
        public GeneTracker_XylXenos XGeneTracker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : PawnExtraData<GeneTracker_XylXenos>.Get(pawn);
        }
    }
}
