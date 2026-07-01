// ReSharper disable ForCanBeConvertedToForeach
namespace Xylib;

public class GeneComp
{
    public Pawn Pawn => parent.pawn;
    public bool Active => parent.Active;

    private static readonly Dictionary<Type, bool> hasTickCache = new();
    private static readonly Dictionary<Type, bool> hasTickIntervalCache = new();

    [Unsaved] public GeneWithComps parent;
    [Unsaved] public GeneCompProperties props;

    [Unsaved] public readonly bool hasTick;
    [Unsaved] public readonly bool hasTickInterval;

    public GeneComp()
    {
        var type = GetType();

        if (!hasTickCache.TryGetValue(type, out hasTick))
        {
            hasTickCache[type] = hasTick = ReflectionHelpers.HasOverridingMethod(type, typeof(GeneComp), nameof(CompTick));
        }

        if (!hasTickIntervalCache.TryGetValue(type, out hasTickInterval))
        {
            hasTickIntervalCache[type]
                = hasTickInterval = ReflectionHelpers.HasOverridingMethod(type, typeof(GeneComp), nameof(CompTickInterval));
        }
    }

    public virtual void CompPostMake()
    {
    }

    public virtual void CompExposeData()
    {
    }

    public virtual void CompPostPostAdd()
    {
    }

    public virtual void CompPostPostRemove()
    {
    }

    public virtual void CompTickInterval(int delta)
    {
    }

    public virtual void CompTick()
    {
    }

    public virtual IEnumerable<Gizmo> CompGetGizmos()
    {
        return [];
    }

    /// <summary>
    ///     Get stats which are displayed on the pawn having this gene.
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest request)
    {
        return [];
    }

    public virtual void CompReset()
    {
    }

    public bool CompAllowActive()
    {
        return true;
    }
}

public class GeneWithComps : Gene, IEventListener
{
    [NotNull]
    public DefModExtension_GeneWithComps DefExt => field ??= def.Extension_GeneWithComps!;

    public GeneType GeneType => geneTypeInternal ??= pawn.genes.Xenogenes.Contains(this) ? GeneType.Xenogene : GeneType.Endogene;

    [Unsaved] private GeneType? geneTypeInternal;
    [Unsaved] private bool activeStateNeedsUpdating = true;

    [CanBeNull] public List<GeneComp> comps;

    private event Action CompTick;
    private event Action<int> CompTickInterval;

    [field: Unsaved]
    public override bool Active
    {
        get
        {
            if (activeStateNeedsUpdating)
            {
                field = CheckActive();
                activeStateNeedsUpdating = false;
            }

            return field;
        }
    }

    protected virtual bool CheckActive()
    {
        if (!base.Active)
            return false;

        if (DefExt.gender != null && DefExt.gender != pawn.gender)
            return false;
        if (DefExt.geneType != null && DefExt.geneType != GeneType)
            return false;

        if (comps != null)
        {
            foreach (var comp in comps)
            {
                if (!comp.CompAllowActive())
                    return false;
            }
        }

        return true;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        if (Scribe.mode == LoadSaveMode.LoadingVars)
            InitializeComps();
        if (comps != null)
        {
            foreach (GeneComp comp in comps)
                comp.CompExposeData();
        }
    }

    public override void PostMake()
    {
        base.PostMake();
        InitializeComps();

        if (comps == null)
            return;

        for (int num = comps.Count - 1; num >= 0; num--)
        {
            try
            {
                comps[num].CompPostMake();
            }
            catch (Exception ex)
            {
                Log.Error("Error in GeneComp.CompPostMake(): " + ex);
            }
        }
    }

    private void InitializeComps()
    {
        var compProperties = DefExt.comps;
        if (compProperties == null)
            return;

        comps = [];
        foreach (GeneCompProperties compProps in compProperties)
        {
            if (compProps.compClass == null)
                continue;

            GeneComp comp = null;
            try
            {
                comp = (GeneComp)Activator.CreateInstance(compProps.compClass);
                comp.props = compProps;
                comp.parent = this;
                comps.Add(comp);
                if (comp.hasTick)
                    CompTick += comp.CompTick;
                if (comp.hasTickInterval)
                    CompTickInterval += comp.CompTickInterval;
            }
            catch (Exception ex)
            {
                Log.Error("Could not instantiate or initialize a GeneComp: " + ex);
                comps.Remove(comp);
            }
        }
    }

    public void RemoveInvalidChemicalHediffs()
    {
        HashSet<HediffDef> hediffDefsToRemove = [];

        foreach (var chemicalDef in DefDatabase<ChemicalDef>.AllDefs)
        {
            if (!pawn.ChemicalIsAllowedByGenes(chemicalDef))
            {
                if (chemicalDef.toleranceHediff != null)
                    hediffDefsToRemove.Add(chemicalDef.toleranceHediff);
                if (chemicalDef.addictionHediff != null)
                    hediffDefsToRemove.Add(chemicalDef.addictionHediff);
            }
        }

        var hediffs = new List<Hediff>(pawn.health.hediffSet.hediffs);
        foreach (var hediff in hediffs)
        {
            if (hediffDefsToRemove.Contains(hediff.def))
                pawn.health.RemoveHediff(hediff);
        }
    }

    public override void PostAdd()
    {
        RemoveInvalidChemicalHediffs();

        if (DefExt.hasPsycast)
            pawn.psychicEntropy.SetInitialPsyfocusLevel();

        base.PostAdd();

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompPostPostAdd();
        }
    }

    public override void PostRemove()
    {
        EventManager.Instance.RemoveListener(this);

        RemoveInvalidChemicalHediffs();

        base.PostRemove();

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompPostPostRemove();
        }
    }

    public override void Reset()
    {
        base.Reset();

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompReset();
        }
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        return SpecialDisplayStats(StatRequest.ForEmpty());
    }

    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest request)
    {
        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var result in comp.SpecialDisplayStats(request))
                yield return result;
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (!Active)
            return;

        CompTickInterval?.Invoke(delta);

        if (!DefExt.hediffGivers.NullOrEmpty() && pawn.IsHashIntervalTick(60, delta))
        {
            for (var index = 0; index < DefExt.hediffGivers.Count; index++)
            {
                HediffGiver hediffGiver = DefExt.hediffGivers[index];
                hediffGiver.OnIntervalPassed(pawn, null);
            }
        }
    }

    public override void Tick()
    {
        base.Tick();

        if (!Active)
            return;

        CompTick?.Invoke();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var gizmo in comp.CompGetGizmos())
                yield return gizmo;
        }

        if (!DebugSettings.ShowDevGizmos)
            yield break;
        if (DefExt.hediffGivers == null)
            yield break;

        for (var index = 0; index < DefExt.hediffGivers.Count; index++)
        {
            HediffGiver hediffGiver = DefExt.hediffGivers[index];
            yield return new Command_Action
            {
                defaultLabel = $"DEV: Trigger {Label} ({hediffGiver.hediff.label}) #{index}",
                action = () => hediffGiver.TryApply(pawn),
                groupable = false,
            };
        }
    }

    public T GetComp<T>() where T : GeneComp
    {
        if (comps == null)
            return null;

        for (var index = 0; index < comps.Count; index++)
        {
            if (comps[index] is T t)
                return t;
        }

        return null;
    }

    public void SetActiveStateNeedsUpdating()
    {
        activeStateNeedsUpdating = true;
    }

    public virtual void RegisterWith(EventManager manager)
    {
        // The only things that can change when a gene is active in the base game are:
        // * The pawn's age changes
        // * The pawn's mutant status changes, which can only happen when the pawn's hediffs change or the pawn dies
        // * An overriding gene is added or removed
        // These cover all of those possibilities.
        manager.Register(EventDefOf.PostGenesChanged, pawn, SetActiveStateNeedsUpdating);
        manager.Register(EventDefOf.PostHediffsChanged, pawn, SetActiveStateNeedsUpdating);
        manager.Register(EventDefOf.PostPawnKilled, pawn, SetActiveStateNeedsUpdating);
        manager.Register(EventDefOf.PostBirthday, pawn, SetActiveStateNeedsUpdating);

        if (comps == null)
            return;

        foreach (var comp in comps.OfType<IEventListener>())
            manager.AddListener(comp);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
        if (comps == null)
            return;

        foreach (var comp in comps.OfType<IEventListener>())
            manager.RemoveListener(comp);
    }
}
