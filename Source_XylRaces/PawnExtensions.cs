using RimWorld.Planet;

namespace XylXenos;

public static class PawnExtensions
{
    extension(Pawn pawn)
    {
        public bool HasActivePsycastGene => pawn.GeneTracker?.hasPsycast == true;

        public Hediff LactationHediff => pawn.HediffsWithComp<HediffComp_Lactating>().FirstOrDefault();

        public GeneAndHediffCache GeneAndHediffCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GeneAndHediffCache.DataManager.Get(pawn);
        }

        [CanBeNull]
        public GeneTracker GeneTracker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : GeneTracker.DataManager.Get(pawn);
        }

        public bool NeedsPsyfocus
        {
            get
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> GenesOfDef(GeneDef def)
        {
            if (pawn.genes == null)
                return [];

            return pawn.GeneAndHediffCache.GetGenesWithDef(def);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGene(GeneDef def)
        {
            if (pawn.genes == null || def == null)
                return false;

            foreach (Gene g in pawn.GenesOfDef(def))
            {
                if (g.Active)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> GenesOfType<T>() where T : class
        {
            if (pawn.genes == null)
                return [];

            return pawn.GeneAndHediffCache.GetGenesOfType<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<T> ActiveGenesOfType<T>() where T : class
        {
            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    yield return g;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstActiveGeneOfType<T>() where T : class
        {
            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    return g;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGeneOfType<T>() where T : class
        {
            if (pawn.genes == null)
                return false;

            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGeneOfType<T>(Func<T, bool> predicate) where T : class
        {
            if (pawn.genes == null)
                return false;

            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active && predicate(g))
                    return true;
            }

            return false;
        }

        public int GetGeneticPsylinkLevelFor(AbilityDef def)
        {
            if (pawn.genes != null && pawn.genes.GenesListForReading.Any(gene =>
                    gene.Active && gene.def.DefExt?.hasPsycast == true && gene.def.abilities?.Any(abilityDef => abilityDef == def) == true))
            {
                return def.level;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> HediffsOfType<T>() where T : class
        {
            return pawn.GeneAndHediffCache.GetHediffsOfType<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<HediffWithComps> HediffsWithComp<T>() where T : class
        {
            return pawn.GeneAndHediffCache.GetHediffsWithComp<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def)
        {
            return pawn.GeneAndHediffCache.GetHediffsWithDef(def);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Hediff> HediffsWithModExtension<T>() where T : class
        {
            return pawn.GeneAndHediffCache.GetHediffsWithModExtension<T>();
        }

        public bool ChemicalIsAllowedByGenes(ChemicalDef chemicalDef)
        {
            var defExtension = chemicalDef?.GetModExtension<DefModExtension_Chemical>();
            if (defExtension == null)
                return true;

            if (!defExtension.prohibitedGenes.NullOrEmpty() && defExtension.prohibitedGenes.Any(pawn.HasActiveGene))
                return false;
            if (!defExtension.requiredGenesAll.NullOrEmpty() && !defExtension.requiredGenesAll.All(pawn.HasActiveGene))
                return false;
            if (!defExtension.requiredGenesAny.NullOrEmpty() && !defExtension.requiredGenesAny.Any(pawn.HasActiveGene))
                return false;

            return true;
        }

        public bool ChemicalIsAllowedByGenes(ThingDef drug)
        {
            ChemicalDef chemical = DrugStatsUtility.GetChemical(drug);
            if (chemical == null)
                return true;

            return pawn.ChemicalIsAllowedByGenes(chemical);
        }
    }
}
