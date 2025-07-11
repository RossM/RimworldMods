using System;
using RimWorld;
using RimWorld.Planet;
using Verse;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    public static class PsyHelpers
    {
        public static int GetPsylinkLevelFor(this Pawn pawn, AbilityDef def)
        {
            using (new ProfileBlock())
            {
                if (pawn.genes != null)
                {
                    if (pawn.genes.GenesListForReading.Any(gene =>
                            gene.def.abilities?.Any(abilityDef => abilityDef == def) == true))
                    {
                        return Math.Max(pawn.GetPsylinkLevel(), def.level);
                    }
                }

                return pawn.GetPsylinkLevel();
            }
        }

        public static bool HasPsycastGene(this Pawn pawn)
        {
            return pawn.HasGeneOfType<Gene_Psycast>();
        }

        public static bool NeedsPsyfocus(this Pawn pawn)
        {
            if (pawn.psychicEntropy.Psylink == null && !pawn.HasPsycastGene())
                return false;
            if (pawn.Suspended)
                return false;
            if (!pawn.Spawned && !pawn.IsCaravanMember())
                return false;
            return true;
        }
    }
}
