// ReSharper disable ForCanBeConvertedToForeach

namespace Xylib;

/// <summary>
///     Represents a component that adds functionality to a <see cref="GeneWithComps" />.
/// </summary>
/// <remarks>
///     <para>
///         Components for a gene are defined in XML. Define a <see cref="GeneCompProperties" /> subclass and add it to the
///         gene's <see cref="DefModExtension_GeneWithComps.comps" /> list. The component will be instantiated
///         automatically when the gene is instantiated.
///     </para>
///     <para>
///         Components can add behavior by overriding callback methods, and can add additional data that saves with the
///         gene by overriding <see cref="CompExposeData" />.
///     </para>
/// </remarks>
[PublicAPI]
public class GeneComp
{
    /// <summary>
    ///     The pawn this gene is attached to. This is a shortcut for <c>parent.pawn</c>.
    /// </summary>
    public Pawn Pawn => parent.pawn;

    /// <summary>
    ///     Whether the gene is active. This is a shortcut for <c>parent.Active</c>.
    /// </summary>
    public bool Active => parent.Active;

    /// <summary>
    ///     The gene this component is attached to.
    /// </summary>
    // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
    [Unsaved] public required GeneWithComps parent;

    /// <summary>
    ///     The properties for this component, as defined in XML.
    /// </summary>
    // ReSharper disable once NotNullOrRequiredMemberIsNotInitialized
    [Unsaved] public required GeneCompProperties props;

    /// <summary>
    ///     Called after the gene is created and initialized, but before it is added to the pawn.
    /// </summary>
    public virtual void CompPostMake()
    {
    }

    /// <summary>
    ///     Called when the game is saving or loading the gene. Override this method to save and load any data in the
    ///     component.
    /// </summary>
    public virtual void CompExposeData()
    {
    }

    /// <summary>
    ///     Called after the gene is added to a pawn.
    /// </summary>
    public virtual void CompPostPostAdd()
    {
    }

    /// <summary>
    ///     Called after the gene is removed from a pawn.
    /// </summary>
    public virtual void CompPostPostRemove()
    {
    }

    /// <summary>
    ///     Called periodically on gameplay tick. The exact tick rate is determined by the game, but is typically no more than
    ///     once every 15 ticks.
    /// </summary>
    /// <remarks>
    ///     It's recommended to use <see cref="Gen.IsHashIntervalTick(Thing, int)" /> to perform actions at a longer interval,
    ///     to avoid performance issues.
    /// </remarks>
    /// <param name="delta"></param>
    public virtual void CompTickInterval(int delta)
    {
    }

    /// <summary>
    ///     Called every gameplay tick.
    /// </summary>
    /// <remarks>
    ///     This can have a significant impact on performance. Prefer to use <see cref="CompTickInterval" /> if possible.
    /// </remarks>
    public virtual void CompTick()
    {
    }

    /// <summary>
    ///     Gets UI gizmos that will be displayed when the pawn is selected.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<Gizmo> CompGetGizmos()
    {
        return [];
    }

    /// <summary>
    ///     Get stats which are displayed on the pawn having this gene.
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        return [];
    }

    /// <summary>
    ///     Called when the pawn's health state is being reset, such as when an old pawn is brought back in a new role.
    ///     This should set the state of the gene component to its initial state.
    /// </summary>
    public virtual void CompReset()
    {
    }

    /// <summary>
    ///     Determines whether this component allows the gene to be active. If any component returns false, the gene will be
    ///     inactive.
    /// </summary>
    /// <returns></returns>
    public bool CompAllowActive()
    {
        return true;
    }
}

/// <summary>
///     A gene whose behavior is defined by <see cref="GeneComp" /> instances.
/// </summary>
[PublicAPI]
public class GeneWithComps : Gene, IEventListener
{
    /// <summary>
    ///     Gets the <see cref="DefModExtension_GeneWithComps" /> for this gene.
    /// </summary>
    public DefModExtension_GeneWithComps DefExt => field ??= def.Extension_GeneWithComps!;

    /// <summary>
    ///     Whether this gene is an endogene or xenogene.
    /// </summary>
    public GeneType GeneType => geneTypeInternal ??= pawn.genes.Xenogenes.Contains(this) ? GeneType.Xenogene : GeneType.Endogene;

    private static readonly Dictionary<Type, bool> hasTickCache = new();
    private static readonly Dictionary<Type, bool> hasTickIntervalCache = new();

    [Unsaved] private GeneType? geneTypeInternal;
    [Unsaved] private bool activeStateNeedsUpdating = true;


    /// <summary>
    ///     The components for this gene.
    /// </summary>
    public List<GeneComp>? comps;

    /// <summary>
    ///     Whether the gene is currently active. Inactive genes shouldn't have any effect on the pawn.
    /// </summary>
    /// <remarks>
    ///     For performance reasons, the value of <see cref="Active" /> is cached, and only updated when the pawn's genes or
    ///     hediffs change, or when the pawn has a birthday.
    ///     If you need to force an update of the active state, call <see cref="SetActiveStateNeedsUpdating" />.
    /// </remarks>
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

    private event Action? CompTick;
    private event Action<int>? CompTickInterval;

    /// <summary>
    ///     Called when updating <see cref="Active" />.
    /// </summary>
    /// <returns></returns>
    protected virtual bool CheckActive()
    {
        if (!base.Active)
            return false;

        if (!DefExt.ValidFor(pawn, GeneType))
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

    /// <summary>
    ///     Called when the game is being saved or loaded.
    /// </summary>
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

    /// <summary>
    ///     Called after the gene is created.
    /// </summary>
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
            Type? compClass = compProps.compClass;
            if (compClass == null)
                continue;

            GeneComp? comp = null;
            try
            {
                comp = (GeneComp)Activator.CreateInstance(compClass);
                comp.props = compProps;
                comp.parent = this;
                comps.Add(comp);

                if (!hasTickCache.TryGetValue(compClass, out var hasTick))
                {
                    hasTickCache[compClass]
                        = hasTick = ReflectionHelpers.HasOverridingMethod(compClass, typeof(GeneComp), nameof(CompTick));
                }

                if (hasTick)
                    CompTick += comp.CompTick;

                if (!hasTickIntervalCache.TryGetValue(compClass, out var hasTickInterval))
                {
                    hasTickIntervalCache[compClass]
                        = hasTickInterval = ReflectionHelpers.HasOverridingMethod(compClass, typeof(GeneComp), nameof(CompTickInterval));
                }

                if (hasTickInterval)
                    CompTickInterval += comp.CompTickInterval;
            }
            catch (Exception ex)
            {
                Log.Error("Could not instantiate or initialize a GeneComp: " + ex);
                if (comp != null)
                    comps.Remove(comp);
            }
        }
    }

    private void RemoveInvalidChemicalHediffs()
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

    /// <summary>
    ///     Called after the gene is added to a pawn.
    /// </summary>
    public override void PostAdd()
    {
        RemoveInvalidChemicalHediffs();

        base.PostAdd();

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompPostPostAdd();
        }
    }

    /// <summary>
    ///     Called after the gene is removed from a pawn.
    /// </summary>
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

    /// <summary>
    ///     Called when the pawn's health is being reset.
    /// </summary>
    public override void Reset()
    {
        base.Reset();

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.CompReset();
        }
    }

    /// <summary>
    ///     Gets stats which are displayed on the pawn's description screen.
    /// </summary>
    /// <returns></returns>
    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var result in comp.SpecialDisplayStats())
                yield return result;
        }
    }

    /// <summary>
    ///     Called periodically on gameplay tick.
    /// </summary>
    /// <param name="delta"></param>
    public override void TickInterval(int delta)
    {
        base.TickInterval(delta);

        if (!Active)
            return;

        CompTickInterval?.Invoke(delta);

        List<HediffGiver>? hediffGivers = DefExt.hediffGivers;
        if (hediffGivers is { Count: > 0 } && pawn.IsHashIntervalTick(60, delta))
        {
            for (var index = 0; index < hediffGivers.Count; index++)
            {
                hediffGivers[index]?.OnIntervalPassed(pawn, null);
            }
        }
    }

    /// <summary>
    ///     Called on gameplay tick.
    /// </summary>
    public override void Tick()
    {
        base.Tick();

        if (!Active)
            return;

        CompTick?.Invoke();
    }

    /// <summary>
    ///     Gets UI gizmos added to the pawn when it is selected.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    ///     Gets the first component with type assignable to <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>The component, or null if there is no matching component</returns>
    public T? GetComp<T>() where T : GeneComp
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

    /// <summary>
    ///     Marks the gene's <see cref="Active" /> property as stale.
    /// </summary>
    public void SetActiveStateNeedsUpdating()
    {
        activeStateNeedsUpdating = true;
    }

    /// <inheritdoc />
    public virtual void RegisterWith(EventManager manager)
    {
        // The only things that can change when a gene is active in the base game are:
        // * The pawn's age changes
        // * The pawn's mutant status changes
        // * An overriding gene is added or removed
        // These cover all of those possibilities.
        manager.Register(EventDefOf.PostGenesChanged, pawn, SetActiveStateNeedsUpdating);
        manager.Register(EventDefOf.PostMutated, pawn, SetActiveStateNeedsUpdating);
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
