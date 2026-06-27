using System.Reflection;
using RimWorld.Planet;

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable UnusedMember.Global

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
        public DefModExtension_GeneWithComps DefExt
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!defExtCache.TryGetValue(gene.index, out DefModExtension_GeneWithComps defExt))
                {
                    defExt = gene.GetModExtension<DefModExtension_GeneWithComps>();
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
        public GeneTracker_GeneWithComps GeneTracker
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : PawnExtraData<GeneTracker_GeneWithComps>.Get(pawn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> AllGenesOfDef(GeneDef def) => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesWithDef(def);

        public bool HasActiveGene(GeneDef def)
        {
            if (def == null)
                return false;

            foreach (Gene gene in pawn.AllGenesOfDef(def))
            {
                if (gene.Active)
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> AllGenesOfType<T>() where T : Gene => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesOfType<T>();

        public IEnumerable<T> ActiveGenesOfType<T>() where T : Gene
        {
            foreach (T gene in pawn.AllGenesOfType<T>())
            {
                if (gene.Active)
                    yield return gene;
            }
        }

        public IEnumerable<T> ActiveGenesOfType<T>(Func<T, bool> predicate) where T : Gene
        {
            foreach (T gene in pawn.AllGenesOfType<T>())
            {
                if (gene.Active && predicate(gene))
                    yield return gene;
            }
        }

        public T FirstActiveGeneOfType<T>() where T : Gene
        {
            foreach (T gene in pawn.AllGenesOfType<T>())
            {
                if (gene.Active)
                    return gene;
            }

            return null;
        }

        public T FirstActiveGeneOfType<T>(Func<T, bool> predicate) where T : Gene
        {
            foreach (T gene in pawn.AllGenesOfType<T>())
            {
                if (gene.Active && predicate(gene))
                    return gene;
            }

            return null;
        }

        public bool HasActiveGeneOfType<T>() where T : Gene
        {
            foreach (T gene in pawn.AllGenesOfType<T>())
            {
                if (gene.Active)
                    return true;
            }

            return false;
        }

        public bool HasActiveGeneOfType<T>(Func<T, bool> predicate) where T : Gene
        {
            foreach (T gene in pawn.AllGenesOfType<T>())
            {
                if (gene.Active && predicate(gene))
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<GeneWithComps> AllGenesWithComp<T>() where T : GeneComp =>
            pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesWithComp<T>();

        public IEnumerable<GeneWithComps> ActiveGenesWithComp<T>() where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active)
                    yield return gene;
            }
        }

        public IEnumerable<GeneWithComps> ActiveGenesWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active && predicate(gene.GetComp<T>()))
                    yield return gene;
            }
        }

        public GeneWithComps FirstActiveGeneWithComp<T>() where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active)
                    return gene;
            }

            return null;
        }

        public GeneWithComps FirstActiveGeneWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active && predicate(gene.GetComp<T>()))
                    return gene;
            }

            return null;
        }

        public bool HasActiveGeneWithComp<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return false;

            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active)
                    return true;
            }

            return false;
        }

        public bool HasActiveGeneWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return false;

            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active && predicate(gene.GetComp<T>()))
                    return true;
            }

            return false;
        }

        public IEnumerable<T> ActiveGeneCompsOfType<T>() where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active)
                    yield return gene.GetComp<T>();
            }
        }

        public IEnumerable<T> ActiveGeneCompsOfType<T>(Func<T, bool> predicate) where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (!gene.Active)
                    continue;

                var comp = gene.GetComp<T>();
                if (predicate(comp))
                    yield return comp;
            }
        }

        public T FirstActiveGeneCompOfType<T>() where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (gene.Active)
                    return gene.GetComp<T>();
            }

            return null;
        }

        public T FirstActiveGeneCompOfType<T>(Func<T, bool> predicate) where T : GeneComp
        {
            foreach (GeneWithComps gene in pawn.AllGenesWithComp<T>())
            {
                if (!gene.Active)
                    continue;

                var comp = gene.GetComp<T>();
                if (predicate(comp))
                    return comp;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> HediffsOfType<T>() where T : Hediff => pawn.GeneAndHediffCache.GetHediffsOfType<T>();

        public IEnumerable<T> HediffsOfType<T>(Func<T, bool> predicate) where T : Hediff => pawn.HediffsOfType<T>().Where(predicate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<HediffWithComps> HediffsWithComp<T>() where T : HediffComp => pawn.GeneAndHediffCache.GetHediffsWithComp<T>();

        public IEnumerable<HediffWithComps> HediffsWithComp<T>(Func<T, bool> predicate) where T : HediffComp
        {
            foreach (HediffWithComps hediff in pawn.HediffsWithComp<T>())
            {
                if (predicate(hediff.GetComp<T>()))
                    yield return hediff;
            }
        }

        public HediffWithComps FirstHediffWithComp<T>() where T : HediffComp => pawn.HediffsWithComp<T>().FirstOrDefault();

        public HediffWithComps FirstHediffWithComp<T>(Func<T, bool> predicate) where T : HediffComp
        {
            foreach (HediffWithComps hediff in pawn.HediffsWithComp<T>())
            {
                if (predicate(hediff.GetComp<T>()))
                    return hediff;
            }

            return null;
        }

        public bool HasHediffWithComp<T>() where T : HediffComp => pawn.HediffsWithComp<T>().Any();

        public bool HasHediffWithComp<T>(Func<T, bool> predicate) where T : HediffComp
        {
            foreach (HediffWithComps hediff in pawn.HediffsWithComp<T>())
            {
                if (predicate(hediff.GetComp<T>()))
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def) => pawn.GeneAndHediffCache.GetHediffsWithDef(def);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def, Func<Hediff, bool> predicate) =>
            pawn.HediffsWithDef(def).Where(predicate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Hediff> HediffsWithModExtension<T>() where T : DefModExtension =>
            pawn.GeneAndHediffCache.GetHediffsWithModExtension<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Hediff> HediffsWithModExtension<T>(Func<T, bool> predicate) where T : DefModExtension
        {
            foreach (Hediff hediff in pawn.HediffsWithModExtension<T>())
            {
                if (predicate(hediff.def.GetModExtension<T>()))
                    yield return hediff;
            }
        }

        public int GetGeneticPsylinkLevelFor(AbilityDef ability)
        {
            if (pawn.GeneTracker?.hasPsycast != true)
                return 0;

            if (pawn.AllGenesOfType<GeneWithComps>().Any(gene =>
                    gene.Active && gene.DefExt.hasPsycast && gene.def.abilities?.Contains(ability) == true))
            {
                return ability.level;
            }

            return 0;
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

    extension(ThingDef thingDef)
    {
        public bool IsRawFoodOrCorpse => thingDef.IsRawHumanFood() || thingDef.IsCorpse;
    }

    extension(MethodInfo method)
    {
        public T CreateDelegate<T>() where T : Delegate
        {
            return (T)method.CreateDelegate(typeof(T));
        }
    }

    extension<T>(T obj)
    {
        public T MemberwiseClone()
        {
            if (memberwiseCloneFn == null)
            {
                var method = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;
                memberwiseCloneFn = method.CreateDelegate<Func<object, object>>();
            }

            return (T)memberwiseCloneFn(obj);
        }
    }

    private static Func<object, object> memberwiseCloneFn;
    public static readonly Dictionary<int, DefModExtension_GeneWithComps> defExtCache = new();
}
