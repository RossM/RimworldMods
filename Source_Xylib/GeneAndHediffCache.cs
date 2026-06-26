using System.Collections;

namespace Xylib;

/// <summary>
/// This class provides fast access to a pawn's genes and hediffs, caching the results of queries for performance.
/// </summary>
public class GeneAndHediffCache : IEventListener, IPawnData
{
    public Pawn Pawn { get; private set; }

    private readonly Dictionary<Type, IList> genesByType = new();
    private readonly Dictionary<GeneDef, List<Gene>> genesByDef = new();
    private readonly Dictionary<Type, List<GeneWithComps>> genesByComp = new();

    private readonly Dictionary<Type, IList> hediffsByType = new();
    private readonly Dictionary<HediffDef, List<Hediff>> hediffsByDef = new();
    private readonly Dictionary<Type, List<Hediff>> hediffsByModExt = new();
    private readonly Dictionary<Type, List<HediffWithComps>> hediffsByComp = new();

    // ReSharper disable once ParameterHidesMember
    void IPawnData.Init(Pawn pawn)
    {
        Pawn = pawn;
        EventManager.Instance.AddListener(this);
    }

    /// <summary>
    /// Gets all of a pawn's genes of a specific type.
    /// </summary>
    /// <typeparam name="T">The <see cref="Gene"/> subclass to find.</typeparam>
    /// <returns>The genes.</returns>
    [NotNull]
    public IEnumerable<T> GetGenesOfType<T>() where T : Gene
    {
        if (genesByType.TryGetValue(typeof(T), out IList value))
            return (List<T>)value;

        value = Pawn.genes?.GenesListForReading.OfType<T>().ToList() ?? [];
        genesByType.Add(typeof(T), value);
        return (List<T>)value;
    }

    /// <summary>
    /// Gets all of a pawn's genes with a specific def. Ordinarily a pawn only has one gene
    /// of a given def, so this will return a single value.
    /// </summary>
    /// <param name="def">The def to find.</param>
    /// <returns>The genes.</returns>
    [NotNull]
    public IEnumerable<Gene> GetGenesWithDef(GeneDef def)
    {
        if (genesByDef.TryGetValue(def, out List<Gene> value))
            return value;

        value = Pawn.genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList() ?? [];
        genesByDef.Add(def, value);
        return value;
    }

    /// <summary>
    /// Gets all of a pawn's genes that derive from <see cref="GeneWithComps"/> and have a <see cref="GeneComp"/> of
    /// the given type.
    /// </summary>
    /// <typeparam name="T">The <see cref="GeneComp"/> subclass to find.</typeparam>
    /// <returns>The genes.</returns>
    [NotNull]
    public IEnumerable<GeneWithComps> GetGenesWithComp<T>() where T : GeneComp
    {
        if (genesByComp.TryGetValue(typeof(T), out List<GeneWithComps> value))
            return value;

        value = Pawn.genes?.GenesListForReading.OfType<GeneWithComps>().Where(g => g.GetComp<T>() != null).ToList() ?? [];
        genesByComp.Add(typeof(T), value);
        return value;
    }

    /// <summary>
    /// Gets all of a pawn's hediffs of a specific type.
    /// </summary>
    /// <typeparam name="T">The <see cref="Hediff"/> subclass to find.</typeparam>
    /// <returns>The hediffs.</returns>
    [NotNull]
    public IEnumerable<T> GetHediffsOfType<T>() where T : Hediff
    {
        if (hediffsByType.TryGetValue(typeof(T), out IList value))
            return (List<T>)value;

        value = Pawn.health.hediffSet.hediffs.OfType<T>().ToList();
        hediffsByType.Add(typeof(T), value);
        return (List<T>)value;
    }

    /// <summary>
    /// Gets all of a pawn's hediffs with a specific def.
    /// </summary>
    /// <param name="def">The def to find.</param>
    /// <returns>The hediffs.</returns>
    [NotNull]
    public IEnumerable<Hediff> GetHediffsWithDef(HediffDef def)
    {
        if (hediffsByDef.TryGetValue(def, out List<Hediff> value))
            return value;

        value = Pawn.health.hediffSet.hediffs.Where(g => g.def == def).ToList();
        hediffsByDef.Add(def, value);
        return value;
    }

    /// <summary>
    /// Gets all of a pawn's hediffs where the def has a specific <see cref="DefModExtension"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="DefModExtension"/> to find.</typeparam>
    /// <returns>The hediffs.</returns>
    [NotNull]
    public IEnumerable<Hediff> GetHediffsWithModExtension<T>() where T : DefModExtension
    {
        if (hediffsByModExt.TryGetValue(typeof(T), out List<Hediff> value))
            return value;

        value = Pawn.health.hediffSet.hediffs.Where(g => g.def.modExtensions?.OfType<T>().Any() == true).ToList();
        hediffsByModExt.Add(typeof(T), value);
        return value;
    }

    /// <summary>
    /// Gets all of pawn's hediffs that are <see cref="HediffWithComps"/> with a comp of the given type.
    /// </summary>
    /// <typeparam name="T">The <see cref="HediffComp"/> subclass to find.</typeparam>
    /// <returns>The hediffs.</returns>
    [NotNull]
    public IEnumerable<HediffWithComps> GetHediffsWithComp<T>() where T : HediffComp
    {
        if (hediffsByComp.TryGetValue(typeof(T), out List<HediffWithComps> value))
            return value;

        value = Pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(hediff => hediff.comps?.OfType<T>().Any() == true)
            .ToList();
        hediffsByComp.Add(typeof(T), value);
        return value;
    }

    public void Notify_PostGenesChanged()
    {
        genesByDef.Clear();
        genesByType.Clear();
        genesByComp.Clear();
    }

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
