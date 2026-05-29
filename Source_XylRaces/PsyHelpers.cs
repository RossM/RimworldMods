using RimWorld;
using RimWorld.Planet;
using Verse;

namespace XylXenos
{
    public static class PsyHelpers
    {
        extension(Pawn pawn)
        {
            public int GetGeneticPsylinkLevelFor(AbilityDef def)
            {
                if (pawn.genes != null && pawn.genes.GenesListForReading.Any(gene =>
                        gene.Active && gene.DefExt()?.hasPsycast == true && gene.def.abilities?.Any(abilityDef => abilityDef == def) == true))
                {
                    return def.level;
                }

                return 0;
            }

            public bool HasActivePsycastGene()
            {
                return pawn.GeneSet()?.hasPsycast == true;
            }

            public bool NeedsPsyfocus()
            {
                // HasPsylink is patched to respect psycast genes
                if (!pawn.HasPsylink)
                    return false;
                if (pawn.Suspended)
                    return false;
                if (!pawn.Spawned && !pawn.IsCaravanMember())
                    return false;
                return true;
            }
        }
    }
}
