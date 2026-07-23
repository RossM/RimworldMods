namespace Xylib;

[UsedFromXml]
[PublicAPI]
public abstract class GeneCompProperties
{
    public Type? compClass;

    public virtual IEnumerable<string> ConfigErrors(GeneDef? gene)
    {
        return PatchHelpers.RequiredMemberErrors(this) ?? [];
    }

    public virtual void ResolveReferences(Def parentDef) { }

    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        return [];
    }

    public virtual IEnumerable<string> CustomEffectDescriptions()
    {
        foreach (var entry in SpecialDisplayStats(StatRequest.ForEmpty()))
        {
            yield return $"{entry.LabelCap}: {entry.ValueString}";
        }
    }
}

[NoReorder]
[UsedFromXml]
[PublicAPI]
public class DefModExtension_GeneWithComps : DefModExtension
{
    public IEnumerable<string> CustomEffectDescriptions => field ??= [.. GetCustomEffectDescriptions()];

    public Texture2D ExtraIcon
    {
        get
        {
            var parentGene = parent as GeneDef;
            DebugAssert.NotNull(parentGene);

            return field ??= extraIconPath is { Length: > 0 }
                ? ContentFinder<Texture2D>.Get(extraIconPath) ?? parentGene.Icon
                : parentGene.Icon;
        }
    }

    #region Properties of the gene itself

    /// <summary>
    ///     If false, this gene won't show up in xenotype creation unless "ignore restrictions" is checked.
    /// </summary>
    public bool showInXenotypeCreation = true;

    /// <summary>
    ///     If non-null, this restricts which genders the gene is active on.
    /// </summary>
    public Gender? gender;

    /// <summary>
    ///     If non-null, this restricts the genes to being only active as an endogene or only active as a xenogene.
    ///     It also hides the gene in xenotype creation unless the correct type of xenotype is being created or
    ///     "ignore restrictions" is checked.
    /// </summary>
    public GeneType? geneType;

    /// <summary>
    ///     The path for an additional icon accessed through the <see cref="ExtraIcon" /> property.
    ///     This is usually used as the icon for a gizmo.
    /// </summary>
    [NoTranslate] public string? extraIconPath;

    #endregion

    #region Properties which are not aggregated in GeneSet

    /// <summary>
    ///     If true, this gene will show up in the policies tab next to the selected drug policy, e.g.
    ///     "Social drugs (drug resistant)".
    /// </summary>
    public bool showInDrugPolicies = false;

    /// <summary>
    ///     Hediff givers which will trigger randomly over time.
    /// </summary>
    public List<HediffGiver>? hediffGivers;

    #endregion

    #region Comps

    public List<GeneCompProperties>? comps;

    #endregion

    #region Properties which are filled automatically and shouldn't be set in XML

    /// <summary>
    ///     The <see cref="GeneDef" /> or <see cref="GeneTemplateDef" /> this object is attached to.
    /// </summary>
    public Def? parent;

    #endregion

    public IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        if (comps is null)
            yield break;

        foreach (var comp in comps)
        {
            if (comp is null)
                continue;

            foreach (var result in comp.SpecialDisplayStats(req))
                yield return result;
        }
    }

    protected virtual IEnumerable<string> GetCustomEffectDescriptions()
    {
        if (comps is null)
            yield break;

        foreach (var comp in comps)
        {
            if (comp is null)
                continue;

            foreach (var result in comp.CustomEffectDescriptions())
                yield return result;
        }
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        DebugAssert.NotNull(parent);

        foreach (var configError in base.ConfigErrors())
            yield return configError;

        var fieldDef = parent?.GetType().GetField("geneClass");
        if (fieldDef is null || fieldDef.FieldType != typeof(Type))
            yield return "parent is not GeneDef or GeneTemplateDef";
        else if (fieldDef.GetValue(parent) is not Type type)
            yield return "geneClass is null or invalid type";
        else if (!typeof(GeneWithComps).IsAssignableFrom(type))
            yield return "geneClass is not GeneExt or subclass thereof";

        if (comps is null)
            yield break;

        foreach (var comp in comps)
        {
            if (comp is null)
            {
                yield return "comp is null";
                continue;
            }

            foreach (var configError in comp.ConfigErrors(parent as GeneDef))
                yield return $"{comp}: {configError}";
        }
    }

    public override void ResolveReferences(Def parentDef)
    {
        // If this is a child of a GeneTemplateDef, we'll be called again with each GeneDef created from it.
        // We need to avoid clobbering an already-set parent.
        if (parent is not null)
            return;

        base.ResolveReferences(parentDef);

        parent = parentDef;

        Extensions.defExtCache.Clear();

        var fieldDef = parentDef.GetType().GetField("geneClass");
        if (fieldDef != null && fieldDef.FieldType == typeof(Type) && (Type?)fieldDef.GetValue(parentDef) == typeof(Gene))
            fieldDef.SetValue(parentDef, typeof(GeneWithComps));

        if (comps is null)
            return;

        foreach (var comp in comps)
        {
            comp?.ResolveReferences(parentDef);
        }
    }

    public T? CompProps<T>() where T : GeneCompProperties
    {
        if (comps is null)
            return null;
        foreach (var comp in comps)
        {
            if (comp is T t)
                return t;
        }

        return null;
    }

    public bool ValidFor(Pawn pawn, GeneType? pawnGeneType)
    {
        if (gender != null && gender != pawn.gender)
            return false;
        if (geneType != null && geneType != pawnGeneType)
            return false;
        return true;
    }
}
