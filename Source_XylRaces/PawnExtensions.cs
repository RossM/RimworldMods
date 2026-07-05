using RimWorld.Planet;

namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public Hediff LactationHediff => pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();

        [CanBeNull]
        public GeneTracker_XylXenos GeneTracker_XylXenos
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : PawnExtraData<GeneTracker_XylXenos>.Get(pawn);
        }

        public bool HasActivePsycastGene => pawn.GeneTracker_XylXenos?.hasPsycast == true;

        public bool NeedsPsyfocus =>
            // HasPsylink is patched to respect psycast genes
            pawn.HasPsylink && !pawn.Suspended && (pawn.Spawned || pawn.IsCaravanMember());

        public int GetGeneticPsylinkLevelFor(AbilityDef ability)
        {
            if (pawn.GeneTracker_XylXenos?.hasPsycast != true)
                return 0;

            if (pawn.AllGenesOfType<GeneWithComps>().Any(gene =>
                    gene.Active && gene.GetComp<GeneComp_Psycast>() != null && gene.def.abilities?.Contains(ability) == true))
            {
                return ability.level;
            }

            return 0;
        }
    }
}
