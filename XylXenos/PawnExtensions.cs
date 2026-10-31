using Waylith.Injection;

namespace XylXenos;

// ReSharper disable MemberCanBeMadeStatic.Local
#pragma warning disable CA1822

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public bool HasActivePsycastGene => pawn.GeneTracker_XylXenos?.hasPsycast is true;

        [InjectedField(Default = null)]
        private ref GeneTracker_XylXenos? GeneTracker_XylXenos_Internal => throw new InvalidOperationException("not injected");

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

            if (pawn.ActiveGenesOfType<GeneWithComps>().Any(gene =>
                    gene.GetComp<GeneComp_Psycast>() != null && gene.def.abilities?.Contains(ability) is true))
                return ability.level;

            return 0;
        }
    }
}
