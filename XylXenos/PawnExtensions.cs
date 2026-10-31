namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public bool HasActivePsycastGene => pawn.GeneTracker_XylXenos?.hasPsycast is true;

        public GeneTracker_XylXenos? GeneTracker_XylXenos
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (pawn.genes == null)
                    return null;
                if (pawn.GeneTracker_XylXenos_Internal is { } tracker)
                    return tracker;
                tracker = pawn.GeneTracker_XylXenos_Internal = new GeneTracker_XylXenos(pawn);
                tracker.Init();
                return tracker;
            }
        }

        public int GetGeneticPsylinkLevelFor(AbilityDef ability)
        {
            if (pawn.GeneTracker_XylXenos?.hasPsycast is not true)
                return 0;

            if (pawn.AllGenesOfType<GeneWithComps>().Any(gene =>
                    gene.Active && gene.GetComp<GeneComp_Psycast>() != null && gene.def.abilities?.Contains(ability) is true))
                return ability.level;

            return 0;
        }
    }
}
