using System.Collections;

namespace XylXenos;

public class GeneAndHediffCache : INotificationListener, IPawnData
{
    public Pawn pawn;

    private readonly Dictionary<Type, IList> genesByType = new();
    private readonly Dictionary<GeneDef, List<Gene>> genesByDef = new();

    private readonly Dictionary<Type, IList> hediffsByType = new();
    private readonly Dictionary<HediffDef, List<Hediff>> hediffsByDef = new();
    private readonly Dictionary<Type, List<Hediff>> hediffsByModExt = new();
    private readonly Dictionary<Type, List<HediffWithComps>> hediffsByComp = new();

    // ReSharper disable once ParameterHidesMember
    public void Init(Pawn pawn)
    {
        this.pawn = pawn;
        RegisterWith(NotificationManager.Instance);
    }

    [NotNull]
    public IEnumerable<T> GetGenesOfType<T>()
    {
        if (genesByType.TryGetValue(typeof(T), out IList value))
            return (List<T>)value;

        value = pawn.genes?.GenesListForReading.OfType<T>().ToList() ?? [];
        genesByType.Add(typeof(T), value);
        return (List<T>)value;
    }

    [NotNull]
    public IEnumerable<Gene> GetGenesWithDef(GeneDef def)
    {
        if (genesByDef.TryGetValue(def, out List<Gene> value))
            return value;

        value = pawn.genes?.GenesListForReading.Where(g => g.def == def).OrderByDescending(g => g.Active).ToList() ?? [];
        genesByDef.Add(def, value);
        return value;
    }

    [NotNull]
    public IEnumerable<T> GetHediffsOfType<T>()
    {
        if (hediffsByType.TryGetValue(typeof(T), out IList value))
            return (List<T>)value;

        value = pawn.health.hediffSet.hediffs.OfType<T>().ToList();
        hediffsByType.Add(typeof(T), value);
        return (List<T>)value;
    }

    [NotNull]
    public IEnumerable<Hediff> GetHediffsWithDef(HediffDef def)
    {
        if (hediffsByDef.TryGetValue(def, out List<Hediff> value))
            return value;

        value = pawn.health.hediffSet.hediffs.Where(g => g.def == def).ToList();
        hediffsByDef.Add(def, value);
        return value;
    }

    [NotNull]
    public IEnumerable<Hediff> GetHediffsWithModExtension<T>() where T : class
    {
        if (hediffsByModExt.TryGetValue(typeof(T), out List<Hediff> value))
            return value;

        value = pawn.health.hediffSet.hediffs.Where(g => g.def.modExtensions?.OfType<T>().Any() == true).ToList();
        hediffsByModExt.Add(typeof(T), value);
        return value;
    }

    [NotNull]
    public IEnumerable<HediffWithComps> GetHediffsWithComp<T>() where T : class
    {
        if (hediffsByComp.TryGetValue(typeof(T), out List<HediffWithComps> value))
            return value;

        value = pawn.health.hediffSet.hediffs.OfType<HediffWithComps>().Where(hediff => hediff.comps?.OfType<T>().Any() == true)
            .ToList();
        hediffsByComp.Add(typeof(T), value);
        return value;
    }

    public void Notify_PostGenesChanged()
    {
        genesByDef.Clear();
        genesByType.Clear();
    }

    public void Notify_PostHediffsChanged()
    {
        hediffsByComp.Clear();
        hediffsByDef.Clear();
        hediffsByModExt.Clear();
        hediffsByType.Clear();
    }

    public void RegisterWith(NotificationManager manager)
    {
        manager.Register(NotificationDefOf.PostGenesChanged, pawn, Notify_PostGenesChanged);
        manager.Register(NotificationDefOf.PostHediffsChanged, pawn, Notify_PostHediffsChanged);
    }

    public void PreUnregister(NotificationManager manager)
    {
    }
}
