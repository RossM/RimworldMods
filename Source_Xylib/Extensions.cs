using System.Reflection;
using RimWorld.Planet;
// ReSharper disable ForCanBeConvertedToForeach

// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable UnusedMember.Global

namespace Xylib;

public static class Extensions
{
    extension(Faction faction)
    {
        /// <summary>
        ///     Gets all living pawns of the faction.
        /// </summary>
        public IEnumerable<Pawn> AllAlivePawns => 
            faction == Faction.OfPlayer ?
                PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction :
                PawnsFinder.AllMapsAndWorld_Alive.Where(pawn => pawn.Faction == faction);
    }

    extension(GeneDef gene)
    {
        /// <summary>
        ///     Gets the <see cref="DefModExtension_GeneWithComps" /> for the gene def, if it exists.
        /// </summary>
        [CanBeNull]
        public DefModExtension_GeneWithComps Extension_GeneWithComps
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

        /// <summary>
        ///     Gets the first <see cref="GeneCompProperties" /> of the specified type for the gene def, if it exists.
        /// </summary>
        /// <typeparam name="T">The <see cref="GeneCompProperties"/> subclass to find.</typeparam>
        /// <returns>The first matching properties object, or null if none exists.</returns>
        public T CompProps<T>() where T : GeneCompProperties => gene.Extension_GeneWithComps?.CompProps<T>();
    }

    extension(Pawn pawn)
    {
        /// <summary>
        ///     Gets the <see cref="GeneAndHediffCache" /> for the pawn.
        /// </summary>
        public GeneAndHediffCache GeneAndHediffCache
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PawnExtraData<GeneAndHediffCache>.Get(pawn);
        }

        /// <summary>
        ///     Gets the <see cref="GeneTracker_GeneWithComps" /> for the pawn, or null if the pawn has no genes.
        /// </summary>
        [CanBeNull]
        public GeneTracker_GeneWithComps GeneTracker_GeneWithComps
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => pawn.genes == null ? null : PawnExtraData<GeneTracker_GeneWithComps>.Get(pawn);
        }

        /// <summary>
        ///     Gets all genes on the pawn with the specified def.
        /// </summary>
        /// <param name="def">
        ///     The gene def to match.
        /// </param>
        /// <returns>
        ///     The matching genes.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Gene> AllGenesOfDef(GeneDef def) => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesWithDef(def);

        /// <summary>
        ///     Determines whether the pawn has an active gene with the specified def.
        /// </summary>
        /// <param name="def">
        ///     The gene def to match.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if the pawn has an active matching gene; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasActiveGene(GeneDef def)
        {
            if (def == null || pawn.genes == null)
                return false;

            IReadOnlyList<Gene> genes = pawn.GeneAndHediffCache.GetGenesWithDef(def);
            for (var index = 0; index < genes.Count; index++)
            {
                Gene gene = genes[index];
                if (gene.Active)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets all genes on the pawn that are assignable to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to return.
        /// </typeparam>
        /// <returns>
        ///     The matching genes.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> AllGenesOfType<T>() where T : Gene => pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesOfType<T>();

        /// <summary>
        ///     Gets all active genes on the pawn that are assignable to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to return.
        /// </typeparam>
        /// <returns>
        ///     The matching active genes.
        /// </returns>
        public IEnumerable<T> ActiveGenesOfType<T>() where T : Gene
        {
            if (pawn.genes == null)
                return [];

            return Iterator();

            IEnumerable<T> Iterator()
            {
                IReadOnlyList<T> genes = pawn.GeneAndHediffCache.GetGenesOfType<T>();
                for (var index = 0; index < genes.Count; index++)
                {
                    T gene = genes[index];
                    if (gene.Active)
                        yield return gene;
                }
            }
        }

        /// <summary>
        ///     Gets all active genes on the pawn that are assignable to the specified type and satisfy a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to return.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching active genes.
        /// </param>
        /// <returns>
        ///     The matching active genes.
        /// </returns>
        public IEnumerable<T> ActiveGenesOfType<T>(Func<T, bool> predicate) where T : Gene
        {
            if (pawn.genes == null)
                return [];

            return Iterator();

            IEnumerable<T> Iterator()
            {
                IReadOnlyList<T> genes = pawn.GeneAndHediffCache.GetGenesOfType<T>();
                for (var index = 0; index < genes.Count; index++)
                {
                    T gene = genes[index];
                    if (gene.Active && predicate(gene))
                        yield return gene;
                }
            }
        }

        /// <summary>
        ///     Gets the first active gene on the pawn that is assignable to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to return.
        /// </typeparam>
        /// <returns>
        ///     The first matching active gene, or <see langword="null" /> if no match is found.
        /// </returns>
        public T FirstActiveGeneOfType<T>() where T : Gene
        {
            if (pawn.genes == null)
                return null;

            IReadOnlyList<T> genes = pawn.GeneAndHediffCache.GetGenesOfType<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                T gene = genes[index];
                if (gene.Active)
                    return gene;
            }

            return null;
        }

        /// <summary>
        ///     Gets the first active gene on the pawn that is assignable to the specified type and satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to return.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching active genes.
        /// </param>
        /// <returns>
        ///     The first matching active gene, or <see langword="null" /> if no match is found.
        /// </returns>
        public T FirstActiveGeneOfType<T>(Func<T, bool> predicate) where T : Gene
        {
            if (pawn.genes == null)
                return null;

            IReadOnlyList<T> genes = pawn.GeneAndHediffCache.GetGenesOfType<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                T gene = genes[index];
                if (gene.Active && predicate(gene))
                    return gene;
            }

            return null;
        }

        /// <summary>
        ///     Determines whether the pawn has an active gene assignable to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     <see langword="true" /> if a matching active gene exists; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasActiveGeneOfType<T>() where T : Gene
        {
            if (pawn.genes == null)
                return false;

            IReadOnlyList<T> genes = pawn.GeneAndHediffCache.GetGenesOfType<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                T gene = genes[index];
                if (gene.Active)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether the pawn has an active gene assignable to the specified type and satisfying a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Gene" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching active genes.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if a matching active gene exists; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasActiveGeneOfType<T>(Func<T, bool> predicate) where T : Gene
        {
            if (pawn.genes == null)
                return false;

            IReadOnlyList<T> genes = pawn.GeneAndHediffCache.GetGenesOfType<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                T gene = genes[index];
                if (gene.Active && predicate(gene))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets all genes on the pawn that have a <see cref="GeneComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     Matching genes with <see cref="GeneComp" /> instances.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<GeneWithComps> AllGenesWithComp<T>() where T : GeneComp =>
            pawn.genes == null ? [] : pawn.GeneAndHediffCache.GetGenesWithComp<T>();

        /// <summary>
        ///     Gets all active genes on the pawn that have a <see cref="GeneComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     Matching active genes with <see cref="GeneComp" /> instances.
        /// </returns>
        public IEnumerable<GeneWithComps> ActiveGenesWithComp<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return [];

            return Iterator();

            IEnumerable<GeneWithComps> Iterator()
            {
                IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
                for (var index = 0; index < genes.Count; index++)
                {
                    GeneWithComps gene = genes[index];
                    if (gene.Active)
                        yield return gene;
                }
            }
        }

        /// <summary>
        ///     Gets all active genes on the pawn that have a <see cref="GeneComp" /> of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="GeneComp" /> instances.
        /// </param>
        /// <returns>
        ///     Matching active genes with <see cref="GeneComp" /> instances.
        /// </returns>
        public IEnumerable<GeneWithComps> ActiveGenesWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return [];

            return Iterator();

            IEnumerable<GeneWithComps> Iterator()
            {
                IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
                for (var index = 0; index < genes.Count; index++)
                {
                    GeneWithComps gene = genes[index];
                    if (gene.Active && predicate(gene.GetComp<T>()))
                        yield return gene;
                }
            }
        }

        /// <summary>
        ///     Gets the first active gene on the pawn that has a <see cref="GeneComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     The first matching active gene, or <see langword="null" /> if no match is found.
        /// </returns>
        public GeneWithComps FirstActiveGeneWithComp<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return null;

            IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                GeneWithComps gene = genes[index];
                if (gene.Active)
                    return gene;
            }

            return null;
        }

        /// <summary>
        ///     Gets the first active gene on the pawn that has a <see cref="GeneComp" /> of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="GeneComp" /> instances.
        /// </param>
        /// <returns>
        ///     The first matching active gene, or <see langword="null" /> if no match is found.
        /// </returns>
        public GeneWithComps FirstActiveGeneWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return null;

            IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                GeneWithComps gene = genes[index];
                if (gene.Active && predicate(gene.GetComp<T>()))
                    return gene;
            }

            return null;
        }

        /// <summary>
        ///     Determines whether the pawn has an active gene with a <see cref="GeneComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     <see langword="true" /> if a matching active gene exists; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasActiveGeneWithComp<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return false;

            IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                GeneWithComps gene = genes[index];
                if (gene.Active)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Determines whether the pawn has an active gene with a <see cref="GeneComp" /> of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="GeneComp" /> instances.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if a matching active gene exists; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasActiveGeneWithComp<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return false;

            IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                GeneWithComps gene = genes[index];
                if (gene.Active && predicate(gene.GetComp<T>()))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets <see cref="GeneComp" /> instances of the specified type from the pawn's active genes.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to return.
        /// </typeparam>
        /// <returns>
        ///     The matching <see cref="GeneComp" /> instances.
        /// </returns>
        public IEnumerable<T> ActiveGeneCompsOfType<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return [];

            return Iterator();

            IEnumerable<T> Iterator()
            {
                IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
                for (var index = 0; index < genes.Count; index++)
                {
                    GeneWithComps gene = genes[index];
                    if (gene.Active)
                        yield return gene.GetComp<T>();
                }
            }
        }

        /// <summary>
        ///     Gets <see cref="GeneComp" /> instances of the specified type from the pawn's active genes that satisfy a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to return.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="GeneComp" /> instances.
        /// </param>
        /// <returns>
        ///     The matching <see cref="GeneComp" /> instances.
        /// </returns>
        public IEnumerable<T> ActiveGeneCompsOfType<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return [];

            return Iterator();

            IEnumerable<T> Iterator()
            {
                IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
                for (var index = 0; index < genes.Count; index++)
                {
                    GeneWithComps gene = genes[index];
                    if (!gene.Active)
                        continue;

                    var comp = gene.GetComp<T>();
                    if (predicate(comp))
                        yield return comp;
                }
            }
        }

        /// <summary>
        ///     Gets the first <see cref="GeneComp" /> of the specified type from the pawn's active genes.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to return.
        /// </typeparam>
        /// <returns>
        ///     The first matching <see cref="GeneComp" />, or <see langword="null" /> if no match is found.
        /// </returns>
        public T FirstActiveGeneCompOfType<T>() where T : GeneComp
        {
            if (pawn.genes == null)
                return null;

            IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                GeneWithComps gene = genes[index];
                if (gene.Active)
                    return gene.GetComp<T>();
            }

            return null;
        }

        /// <summary>
        ///     Gets the first <see cref="GeneComp" /> of the specified type from the pawn's active genes that satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="GeneComp" /> subclass to return.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="GeneComp" /> instances.
        /// </param>
        /// <returns>
        ///     The first matching <see cref="GeneComp" />, or <see langword="null" /> if no match is found.
        /// </returns>
        public T FirstActiveGeneCompOfType<T>(Func<T, bool> predicate) where T : GeneComp
        {
            if (pawn.genes == null)
                return null;

            IReadOnlyList<GeneWithComps> genes = pawn.GeneAndHediffCache.GetGenesWithComp<T>();
            for (var index = 0; index < genes.Count; index++)
            {
                GeneWithComps gene = genes[index];
                if (!gene.Active)
                    continue;

                var comp = gene.GetComp<T>();
                if (predicate(comp))
                    return comp;
            }

            return null;
        }

        /// <summary>
        ///     Gets all hediffs on the pawn that are assignable to the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Hediff" /> subclass to return.
        /// </typeparam>
        /// <returns>
        ///     The matching hediffs.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<T> HediffsOfType<T>() where T : Hediff => pawn.GeneAndHediffCache.GetHediffsOfType<T>();

        /// <summary>
        ///     Gets all hediffs on the pawn that are assignable to the specified type and satisfy a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Hediff" /> subclass to return.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching hediffs.
        /// </param>
        /// <returns>
        ///     The matching hediffs.
        /// </returns>
        public IEnumerable<T> HediffsOfType<T>(Func<T, bool> predicate) where T : Hediff => pawn.HediffsOfType<T>().Where(predicate);

        /// <summary>
        ///     Gets all hediffs on the pawn that have a <see cref="HediffComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="HediffComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     Matching hediffs with <see cref="HediffComp" /> instances.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<HediffWithComps> HediffsWithComp<T>() where T : HediffComp => pawn.GeneAndHediffCache.GetHediffsWithComp<T>();

        /// <summary>
        ///     Gets all hediffs on the pawn that have a <see cref="HediffComp" /> of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="HediffComp" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="HediffComp" /> instances.
        /// </param>
        /// <returns>
        ///     Matching hediffs with <see cref="HediffComp" /> instances.
        /// </returns>
        public IEnumerable<HediffWithComps> HediffsWithComp<T>(Func<T, bool> predicate) where T : HediffComp
        {
            IReadOnlyList<HediffWithComps> hediffs = pawn.GeneAndHediffCache.GetHediffsWithComp<T>();
            for (var index = 0; index < hediffs.Count; index++)
            {
                HediffWithComps hediff = hediffs[index];
                if (predicate(hediff.GetComp<T>()))
                    yield return hediff;
            }
        }

        /// <summary>
        ///     Gets the first hediff on the pawn that has a <see cref="HediffComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="HediffComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     The first matching hediff, or <see langword="null" /> if no match is found.
        /// </returns>
        public HediffWithComps FirstHediffWithComp<T>() where T : HediffComp => pawn.HediffsWithComp<T>().FirstOrDefault();

        /// <summary>
        ///     Gets the first hediff on the pawn that has a <see cref="HediffComp" /> of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="HediffComp" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="HediffComp" /> instances.
        /// </param>
        /// <returns>
        ///     The first matching hediff, or <see langword="null" /> if no match is found.
        /// </returns>
        public HediffWithComps FirstHediffWithComp<T>(Func<T, bool> predicate) where T : HediffComp
        {
            IReadOnlyList<HediffWithComps> hediffs = pawn.GeneAndHediffCache.GetHediffsWithComp<T>();
            for (var index = 0; index < hediffs.Count; index++)
            {
                HediffWithComps hediff = hediffs[index];
                if (predicate(hediff.GetComp<T>()))
                    return hediff;
            }

            return null;
        }

        /// <summary>
        ///     Determines whether the pawn has a hediff with a <see cref="HediffComp" /> of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="HediffComp" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     <see langword="true" /> if a matching hediff exists; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasHediffWithComp<T>() where T : HediffComp => pawn.HediffsWithComp<T>().Any();

        /// <summary>
        ///     Determines whether the pawn has a hediff with a <see cref="HediffComp" /> of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="HediffComp" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching <see cref="HediffComp" /> instances.
        /// </param>
        /// <returns>
        ///     <see langword="true" /> if a matching hediff exists; otherwise, <see langword="false" />.
        /// </returns>
        public bool HasHediffWithComp<T>(Func<T, bool> predicate) where T : HediffComp
        {
            IReadOnlyList<HediffWithComps> hediffs = pawn.GeneAndHediffCache.GetHediffsWithComp<T>();
            for (var index = 0; index < hediffs.Count; index++)
            {
                HediffWithComps hediff = hediffs[index];
                if (predicate(hediff.GetComp<T>()))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets all hediffs on the pawn with the specified def.
        /// </summary>
        /// <param name="def">
        ///     The hediff def to match.
        /// </param>
        /// <returns>
        ///     The matching hediffs.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def) => pawn.GeneAndHediffCache.GetHediffsWithDef(def);

        /// <summary>
        ///     Gets all hediffs on the pawn with the specified def that satisfy a predicate.
        /// </summary>
        /// <param name="def">
        ///     The hediff def to match.
        /// </param>
        /// <param name="predicate">
        ///     The predicate used to filter matching hediffs.
        /// </param>
        /// <returns>
        ///     The matching hediffs.
        /// </returns>
        public IEnumerable<Hediff> HediffsWithDef(HediffDef def, Func<Hediff, bool> predicate) =>
            pawn.HediffsWithDef(def).Where(predicate);

        /// <summary>
        ///     Gets all hediffs on the pawn whose defs have a mod extension of the specified type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="DefModExtension" /> subclass to match.
        /// </typeparam>
        /// <returns>
        ///     The matching hediffs.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Hediff> HediffsWithModExtension<T>() where T : DefModExtension =>
            pawn.GeneAndHediffCache.GetHediffsWithModExtension<T>();

        /// <summary>
        ///     Gets all hediffs on the pawn whose defs have a mod extension of the specified type which satisfies a predicate.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="DefModExtension" /> subclass to match.
        /// </typeparam>
        /// <param name="predicate">
        ///     The predicate used to filter matching mod extensions.
        /// </param>
        /// <returns>
        ///     The matching hediffs.
        /// </returns>
        public IEnumerable<Hediff> HediffsWithModExtension<T>(Func<T, bool> predicate) where T : DefModExtension
        {
            IReadOnlyList<Hediff> hediffs = pawn.GeneAndHediffCache.GetHediffsWithModExtension<T>();
            for (var index = 0; index < hediffs.Count; index++)
            {
                Hediff hediff = hediffs[index];
                if (predicate(hediff.def.GetModExtension<T>()))
                    yield return hediff;
            }
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

    extension(IntRange range)
    {
        public bool Includes(int value)
        {
            return range.min <= value && value <= range.max;
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
