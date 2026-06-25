namespace Xylib;

public class GeneComp
{
    public Pawn Pawn => parent.pawn;
    public bool Active => parent.Active;
    [Unsaved] public GeneWithComps parent;
    [Unsaved] public GeneCompProperties props;

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
        return [];
    }

    public virtual void CompReset()
    {
    }
}

public class GeneWithComps : Gene, IEventListener
{
    [NotNull]
    public DefModExtension_GeneWithComps DefExt => field ??= def.DefExt!;

    public GeneType GeneType => geneTypeInternal ??= pawn.genes.Xenogenes.Contains(this) ? GeneType.Xenogene : GeneType.Endogene;

    [Unsaved] private GeneType? geneTypeInternal;
    [Unsaved] private bool activeFilled;

    [CanBeNull] public List<GeneComp> comps;

    public bool Removed { get; private set; } = false;

    [field: Unsaved]
    public override bool Active
    {
        get
        {
            if (!base.Active)
                return false;
            if (Removed)
                return false;
            if (!activeFilled)
            {
                field = CheckActive();
                activeFilled = true;
            }

            return field;
        }
    }

    private bool CheckActive() =>
        (DefExt.gender == null || DefExt.gender == pawn.gender) &&
        (DefExt.geneType == null || DefExt.geneType == GeneType);

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
        Removed = true;
        EventManager.Instance.UnregisterAll(this);

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

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompTickInterval(delta);
        }

        if (!Active)
            return;
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

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompTick();
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

    public virtual void RegisterWith(EventManager manager)
    {
        if (comps == null)
            return;

        foreach (var comp in comps.OfType<IEventListener>())
            comp.RegisterWith(manager);
    }

    public void PreUnregister(EventManager manager)
    {
        if (comps == null)
            return;

        foreach (var comp in comps.OfType<IEventListener>())
            manager.UnregisterAll(comp);
    }
}
