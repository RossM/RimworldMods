using System.Collections;

namespace Xylib;

/// <summary>
///     Caches common gene and hediff queries for a pawn.
/// </summary>
/// <remarks>
///     Cached gene results are cleared when the pawn's genes change, and cached hediff results are cleared when the pawn's
///     hediffs
///     change.
/// </remarks>
public class GeneAndHediffCache : IEventListener, IPawnData
{
    private readonly Dictionary<Type, IList> genesByType = new();
    private readonly Dictionary<GeneDef, List<Gene>> genesByDef = new();
    private readonly Dictionary<Type, List<GeneWithComps>> genesByComp = new();

    private readonly Dictionary<Type, IList> hediffsByType = new();
    private readonly Dictionary<HediffDef, List<Hediff>> hediffsByDef = new();
    private readonly Dictionary<Type, List<Hediff>> hediffsByModExt = new();
    private readonly Dictionary<Type, List<HediffWithComps>> hediffsByComp = new();

    /// <summary>
    ///     Gets the pawn whose genes and hediffs are cached.
    /// </summary>
    public Pawn Pawn { get; private set; }

    // ReSharper disable once ParameterHidesMember
    void IPawnData.Init(Pawn pawn)
    {
        Pawn = pawn;
        EventManager.Instance.AddListener(this);
    }

    /// <summary>
    ///     Gets the pawn's genes of the specified type.
    /// </summary>
    /// <typeparam name="T">
    ///     The <see cref="Gene" /> subclass to return.
    /// </typeparam>
    /// <returns>
    ///     The matching genes.
    /// </returns>
    [NotNull]
    public IReadOnlyList<T> GetGenesOfType<T>() where T : Gene
    {
        if (genesByType.TryGetValue(typeof(T), out IList value))
            return (List<T>)value;

        value = Pawn.genes?.GenesListForReading.OfType<T>().ToList() ?? [];
        genesByType.Add(typeof(T), value);
        return (List<T>)value;
    }

    /// <summary>
    ///     Gets the pawn's genes with the specified def.
    /// </summary>
    /// <remarks>
    ///     Pawns usually have only one gene for a given def. When multiple genes match, active genes are returned first.
    /// </remarks>
    /// <param name="def">
    ///     The gene def to match.
    /// </param>
    /// <returns>
    ///     The matching genes.
    /// </returns>
    [NotNull]
    public IReadOnlyList<Gene> GetGenesWithDef(GeneDef def)
    {
        if (genesByDef.TryGetValue(def, out List<Gene> value))
            return value;

        value = Pawn.genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList() ?? [];
        genesByDef.Add(def, value);
        return value;
    }

    /// <summary>
    ///     Gets the pawn's genes that have a comp of the specified type.
    /// </summary>
    /// <typeparam name="T">
    ///     The <see cref="GeneComp" /> subclass to match.
    /// </typeparam>
    /// <returns>
    ///     Matching <see cref="GeneWithComps" /> instances.
    /// </returns>
    [NotNull]
    public IReadOnlyList<GeneWithComps> GetGenesWithComp<T>() where T : GeneComp
    {
        if (genesByComp.TryGetValue(typeof(T), out List<GeneWithComps> value))
            return value;

        value = Pawn.genes?.GenesListForReading.OfType<GeneWithComps>().Where(g => g.GetComp<T>() != null).ToList() ?? [];
        genesByComp.Add(typeof(T), value);
        return value;
    }

    /// <summary>
    ///     Gets the pawn's hediffs of the specified type.
    /// </summary>
    /// <typeparam name="T">
    ///     The <see cref="Hediff" /> subclass to return.
    /// </typeparam>
    /// <returns>
    ///     The matching hediffs.
    /// </returns>
    [NotNull]
    public IReadOnlyList<T> GetHediffsOfType<T>() where T : Hediff
    {
        if (hediffsByType.TryGetValue(typeof(T), out IList value))
            return (List<T>)value;

        value = Pawn.health.hediffSet.hediffs.OfType<T>().ToList();
        hediffsByType.Add(typeof(T), value);
        return (List<T>)value;
    }

    /// <summary>
    ///     Gets the pawn's hediffs with the specified def.
    /// </summary>
    /// <param name="def">
    ///     The hediff def to match.
    /// </param>
    /// <returns>
    ///     The matching hediffs.
    /// </returns>
    [NotNull]
    public IReadOnlyList<Hediff> GetHediffsWithDef(HediffDef def)
    {
        if (hediffsByDef.TryGetValue(def, out List<Hediff> value))
            return value;

        value = Pawn.health.hediffSet.hediffs.Where(g => g.def == def).ToList();
        hediffsByDef.Add(def, value);
        return value;
    }

    /// <summary>
    ///     Gets the pawn's hediffs whose defs have the specified mod extension.
    /// </summary>
    /// <typeparam name="T">
    ///     The <see cref="DefModExtension" /> subclass to match.
    /// </typeparam>
    /// <returns>
    ///     The matching hediffs.
    /// </returns>
    [NotNull]
    public IReadOnlyList<Hediff> GetHediffsWithModExtension<T>() where T : DefModExtension
    {
        if (hediffsByModExt.TryGetValue(typeof(T), out List<Hediff> value))
            return value;

        value = Pawn.health.hediffSet.hediffs.Where(g => g.def.modExtensions?.OfType<T>().Any() is true).ToList();
        hediffsByModExt.Add(typeof(T), value);
        return value;
    }

    /// <summary>
    ///     Gets the pawn's hediffs that have a comp of the specified type.
    /// </summary>
    /// <typeparam name="T">
    ///     The <see cref="HediffComp" /> subclass to match.
    /// </typeparam>
    /// <returns>
    ///     Matching <see cref="HediffWithComps" /> instances.
    /// </returns>
    [NotNull]
    public IReadOnlyList<HediffWithComps> GetHediffsWithComp<T>() where T : HediffComp
    {
        if (hediffsByComp.TryGetValue(typeof(T), out List<HediffWithComps> value))
            return value;

        value = Pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(hediff => hediff.comps?.OfType<T>().Any() is true)
            .ToList();
        hediffsByComp.Add(typeof(T), value);
        return value;
    }

    /// <summary>
    ///     Clears cached gene query results after the pawn's genes change.
    /// </summary>
    public void Notify_PostGenesChanged()
    {
        genesByDef.Clear();
        genesByType.Clear();
        genesByComp.Clear();
    }

    /// <summary>
    ///     Clears cached hediff query results after the pawn's hediffs change.
    /// </summary>
    public void Notify_PostHediffsChanged()
    {
        hediffsByComp.Clear();
        hediffsByDef.Clear();
        hediffsByModExt.Clear();
        hediffsByType.Clear();
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostGenesChanged, Pawn, Notify_PostGenesChanged);
        manager.Register(EventDefOf.PostHediffsChanged, Pawn, Notify_PostHediffsChanged);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
