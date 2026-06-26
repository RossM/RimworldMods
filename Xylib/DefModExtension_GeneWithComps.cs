using System.Xml;

namespace Xylib;

public class GeneIngestionThoughtOverride
{
    public ThingDef thing;
    public List<ThoughtDef> thoughts;
    public FoodType allowedFoodTypes;
    public FoodType disallowedFoodTypes;
}

public class RenderNodeModifier
{
    public PawnRenderNodeTagDef tag;
    public float scale = 1.0f;
    public Vector3 offset = Vector3.zero;

    public bool Matches(PawnRenderNode node)
    {
        return node.Props.tagDef == tag;
    }
}

public abstract class GeneCompProperties
{
    public Type compClass;

    public virtual IEnumerable<string> ConfigErrors()
    {
        return [];
    }

    public virtual void ResolveReferences(Def parentDef)
    {
    }

    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        return [];
    }

    public virtual IEnumerable<string> CustomEffectDescriptions()
    {
        return [];
    }
}

[NoReorder]
public class DefModExtension_GeneWithComps : DefModExtension
{
    public IEnumerable<string> CustomEffectDescriptions => field ??= GetCustomEffectDescriptions().ToList();

    public Texture2D ExtraIcon =>
        field ??= extraIconPath.NullOrEmpty()
            ? (parent as GeneDef)?.Icon
            : ContentFinder<Texture2D>.Get(extraIconPath) ?? (parent as GeneDef)?.Icon;

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
    [NoTranslate] [CanBeNull] public string extraIconPath;

    #endregion

    #region Properties which are aggregated in GeneSet for fast access

    /// <summary>
    ///     Scales pawn body size, which affects many things including the chance of being hit by ranged fire.
    /// </summary>
    public float bodySizeFactor = 1.0f;

    /// <summary>
    ///     Scales body part hit points for all body parts.
    /// </summary>
    public float healthScaleFactor = 1.0f;

    /// <summary>
    ///     If true, the pawn will have psychic entropy with or without a psylink, and any psycast
    ///     abilities added by this gene in <see cref="GeneDef.abilities" /> will be usable without
    ///     a psylink.
    /// </summary>
    public bool hasPsycast;

    /// <summary>
    ///     Modifiers to the scale and offset to specific nodes in the pawn's render tree, used to
    ///     change the pawn's visual in a different way than just adding additional nodes.
    /// </summary>
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;

    /// <summary>
    ///     If set, the pawn won't be attacked by pawns of a specific other faction type even if
    ///     those pawns are normally hostile. If any pawn from the carrier's faction attacks a
    ///     pawn or building from the enemy faction, the effect is lost for 2500 ticks (1 in-game
    ///     hour). Tame animals from the pawn's action also benefit from the effect.
    /// </summary>
    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;

    /// <summary>
    ///     Disables thoughts from ingesting foods on a more granular level than just disabling an
    ///     entire thought, for example it can disable the negative thought from eating raw food
    ///     but only for raw meat.
    /// </summary>
    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;

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
    [CanBeNull] public List<HediffGiver> hediffGivers;

    #endregion

    #region Comps

    [CanBeNull] public List<GeneCompProperties> comps;

    #endregion

    #region Properties which are filled automatically and shouldn't be set in XML

    /// <summary>
    ///     The <see cref="GeneDef" /> or <see cref="GeneTemplateDef" /> this object is attached to.
    /// </summary>
    [CanBeNull] public Def parent;

    #endregion

    public IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        if (bodySizeFactor != 1.0f)
        {
            yield return new(StatCategoryDefOf.BasicsPawn, "BodySize".Translate(), bodySizeFactor.ToStringPercent(),
                "Stat_Race_BodySize_Desc".Translate(), 4195);
        }

        if (healthScaleFactor != 1.0f)
        {
            yield return new(StatCategoryDefOf.BasicsPawn, "HitPointsBasic".Translate(), healthScaleFactor.ToStringPercent(),
                "XylHitPointsDesc".Translate(), 4194);
        }

        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var result in comp.SpecialDisplayStats(req))
                yield return result;
        }
    }

    protected virtual IEnumerable<string> GetCustomEffectDescriptions()
    {
        if (bodySizeFactor != 1.0f)
            yield return $"{"BodySize".Translate().CapitalizeFirst()}: {bodySizeFactor.ToStringPercent()}";
        if (healthScaleFactor != 1.0f)
            yield return $"{"HitPointsBasic".Translate().CapitalizeFirst()}: {healthScaleFactor.ToStringPercent()}";

        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var result in comp.CustomEffectDescriptions())
                yield return result;
        }
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
            yield return configError;

        var fieldDef = parent?.GetType().GetField("geneClass");
        if (fieldDef == null || fieldDef.FieldType != typeof(Type))
            yield return "parent is not GeneDef or GeneTemplateDef";
        else if (!typeof(GeneWithComps).IsAssignableFrom((Type)fieldDef.GetValue(parent)))
            yield return "geneClass is not GeneExt or subclass thereof";

        if (comps != null)
        {
            foreach (var comp in comps)
            foreach (var configError in comp.ConfigErrors())
                yield return configError;
        }
    }

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);

        parent = parentDef;

        Extensions.defExtCache.Clear();

        var fieldDef = parentDef.GetType().GetField("geneClass");
        if (fieldDef != null && fieldDef.FieldType == typeof(Type) && (Type)fieldDef.GetValue(parentDef) == typeof(Gene))
            fieldDef.SetValue(parentDef, typeof(GeneWithComps));

        if (comps != null)
        {
            foreach (var comp in comps)
                comp.ResolveReferences(parentDef);
        }
    }

    public T CompProps<T>() where T : GeneCompProperties
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
}
