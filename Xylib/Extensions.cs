using RimWorld.Planet;

namespace Xylib;

public static class Extensions
{
    extension(Faction faction)
    {
        public IEnumerable<Pawn> AllPawns => Find.Maps.SelectMany(map => map.mapPawns.PawnsInFaction(faction));
    }

    extension(GeneDef gene)
    {
        [CanBeNull]
        public DefModExtension_Gene DefExt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!defExtCache.TryGetValue(gene.index, out DefModExtension_Gene defExt))
                {
                    defExt = gene.GetModExtension<DefModExtension_Gene>();
                    defExtCache.Add(gene.index, defExt);
                }

                return defExt;
            }
        }
    }

    extension(Pawn pawn)
    {
        public bool HasActivePsycastGene => pawn.GeneTracker?.hasPsycast == true;

        public bool NeedsPsyfocus =>
            // HasPsylink is patched to respect psycast genes
            pawn.HasPsylink && !pawn.Suspended && (pawn.Spawned || pawn.IsCaravanMember());

        public GeneAndHediffCache GeneAndHediffCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PawnExtraData<GeneAndHediffCache>.Get(pawn);
        }

        [CanBeNull]
        public GeneTracker GeneTracker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : PawnExtraData<GeneTracker>.Get(pawn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> GenesOfDef(GeneDef def) => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesWithDef(def);

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
        public IEnumerable<T> GenesOfType<T>() where T : Gene => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesOfType<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<T> ActiveGenesOfType<T>() where T : Gene
        {
            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    yield return g;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T FirstActiveGeneOfType<T>() where T : Gene
        {
            foreach (T g in pawn.GenesOfType<T>())
            {
                if (((Gene)(object)g).Active)
                    return g;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGeneOfType<T>() where T : Gene
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
        public bool HasActiveGeneOfType<T>(Func<T, bool> predicate) where T : Gene
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<GeneWithComps> GenesWithComp<T>() where T : GeneComp => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesWithComp<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GeneWithComps FirstActiveGeneWithComp<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return null;

            foreach (var g in pawn.GenesWithComp<T>())
            {
                if (g.Active)
                    return g;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGeneWithComp<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return false;

            foreach (var g in pawn.GenesWithComp<T>())
            {
                if (g.Active)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasActiveGeneWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return false;

            foreach (var g in pawn.GenesWithComp<T>())
            {
                if (g.Active && predicate(g.GetComp<T>()))
                    return true;
            }

            return false;
        }

        public T FirstActiveGeneCompOfType<T>() where T : GeneComp => pawn.FirstActiveGeneWithComp<T>()?.GetComp<T>();
        
        public int GetGeneticPsylinkLevelFor(AbilityDef ability)
        {
            if (pawn.GeneTracker?.hasPsycast != true)
                return 0;

            if (pawn.GenesOfType<GeneWithComps>().Any(gene =>
                    gene.Active && gene.DefExt.hasPsycast && gene.def.abilities?.Any(abilityDef => abilityDef == ability) == true))
            {
                return ability.level;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> HediffsOfType<T>() where T : Hediff => pawn.GeneAndHediffCache.GetHediffsOfType<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<HediffWithComps> HediffsWithComp<T>() where T : HediffComp => pawn.GeneAndHediffCache.GetHediffsWithComp<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def) => pawn.GeneAndHediffCache.GetHediffsWithDef(def);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public IEnumerable<Hediff> HediffsWithModExtension<T>() where T : DefModExtension => pawn.GeneAndHediffCache.GetHediffsWithModExtension<T>();

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

    extension(ThingDef thingDef)
    {
        public bool IsRawFoodOrCorpse => thingDef.IsRawHumanFood() || thingDef.IsCorpse;
    }

    public static readonly Dictionary<int, DefModExtension_Gene> defExtCache = new();
}
