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
            hasTickCache[type] = hasTick = type.GetMethod("CompTick")!.DeclaringType != typeof(GeneComp);
        if (!hasTickIntervalCache.TryGetValue(type, out hasTickInterval))
            hasTickIntervalCache[type] = hasTickInterval = type.GetMethod("CompTickInterval")!.DeclaringType != typeof(GeneComp);
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

    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        return props.SpecialDisplayStats(StatRequest.ForEmpty());
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
    public DefModExtension_GeneWithComps DefExt => field ??= def.DefExt!;

    public GeneType GeneType => geneTypeInternal ??= pawn.genes.Xenogenes.Contains(this) ? GeneType.Xenogene : GeneType.Endogene;

    [Unsaved] private GeneType? geneTypeInternal;
    [Unsaved] private bool activeStateNeedsUpdating = true;

    [CanBeNull] public List<GeneComp> comps;

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
                comps.RemoveAt(num);
            }
        }
    }

    private void InitializeComps()
    {
        var compProperties = DefExt.comps;
        if (compProperties == null)
            return;

        comps = new();
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
        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var result in comp.SpecialDisplayStats())
                yield return result;
        }
    }

    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (!Active)
            return;

        if (comps != null)
        {
            foreach (var comp in comps)
            {
                if (comp.hasTickInterval)
                    comp.CompTickInterval(delta);
            }
        }

        if (DefExt.hediffGivers.NullOrEmpty())
            return;
        if (!pawn.IsHashIntervalTick(60, delta))
            return;

        foreach (var hediffGiver in DefExt.hediffGivers)
        {
            hediffGiver.OnIntervalPassed(pawn, null);
        }
    }

    public override void Tick()
    {
        base.Tick();

        if (!Active)
            return;

        if (comps != null)
        {
            foreach (var comp in comps)
            {
                if (comp.hasTick)
                    comp.CompTick();
            }
        }
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
        foreach (var comp in comps)
        {
            if (comp is T t)
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
