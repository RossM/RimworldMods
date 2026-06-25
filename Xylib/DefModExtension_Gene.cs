using System.Xml;

namespace Xylib;

public class GeneIngestionThoughtOverride
{
    public ThingDef thing;
    public List<MeatSourceCategory> meatSources;
    public List<ThoughtDef> thoughts;
}

public class JoyGiverFactor
{
    public JoyGiverDef joyGiver;
    public float factor = 1.0f;

    [UsedImplicitly]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "joyGiver", xmlRoot.Name);
        factor = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
    }
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

public class StartingItemOption
{
    public ThingDef item;
    public FoodTypeFlags foodType;
    public float chance = 1.0f;
    public IntRange count = IntRange.Zero;
    public FloatRange nutritionAmount = FloatRange.Zero;
    public bool ignoreRestrictions;
}

public abstract class GeneCompProperties
{
    public Type compClass;

    public virtual IEnumerable<string> ConfigErrors()
    {
        yield break;
    }

    public virtual void ResolveReferences(Def parentDef)
    {
    }
}

[NoReorder]
public class DefModExtension_Gene : DefModExtension
{
    public IEnumerable<string> CustomEffectDescriptions => field ??= GetCustomEffectDescriptions().ToList();

    public Texture2D ExtraIcon =>
        field ??= extraIconPath.NullOrEmpty()
            ? (parent as GeneDef)?.Icon
            : ContentFinder<Texture2D>.Get(extraIconPath) ?? (parent as GeneDef)?.Icon;

    public string GenderRatioDescription =>
        femaleChance switch
        {
            >= 1.0f => "XylGenderRatioAlwaysFemale".Translate(),
            <= 0.0f => "XylGenderRatioAlwaysMale".Translate(),
            { } chance => "XylGenderRatioValue".Translate(chance.ToStringPercent(),
                (1 - chance).ToStringPercent())
        };

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
    ///     If set, any slave rebellion MTB days higher than this value will be changed to "never".
    /// </summary>
    public float slaveRebellionThresholdDays = float.MaxValue;

    /// <summary>
    ///     A multiplier on the chance of this pawn going manhunter when damaged as a wild man.
    /// </summary>
    public float manhunterOnDamageChanceFactor = 1.0f;

    /// <summary>
    ///     A multiplier on the chance of this pawn going manhunter when a tame attempt fails as a wild man.
    /// </summary>
    public float manhunterOnTameFailChanceFactor = 1.0f;

    /// <summary>
    ///     If true, the pawn will have psychic entropy with or without a psylink, and any psycast
    ///     abilities added by this gene in <see cref="GeneDef.abilities" /> will be usable without
    ///     a psylink.
    /// </summary>
    public bool hasPsycast;

    /// <summary>
    ///     Modifiers to the chances of the pawn selecting certain joy sources.
    /// </summary>
    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;

    /// <summary>
    ///     Additional buildables (e.g. fungal gravel) which this gene enables.
    /// </summary>
    [CanBeNull] public List<BuildableDef> addDesignators;

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
    ///     Affects how genes are inherited. If one parent has a higher total xenotype strength than the other,
    ///     the baby will inherit all the endogenes from that parent and none of the endogenes from the other
    ///     parent.
    /// </summary>
    public int xenotypeStrength;

    /// <summary>
    ///     If non-null, determines the gender ratio of children. Note that it is the baby's genes that
    ///     determine the baby's gender, not the parents'. Also note that only the first gene with this
    ///     set will have an effect.
    /// </summary>
    public float? femaleChance;

    /// <summary>
    ///     If true, this gene will show up in the policies tab next to the selected drug policy, e.g.
    ///     "Social drugs (drug resistant)".
    /// </summary>
    public bool showInDrugPolicies = false;

    /// <summary>
    ///     Hediff givers which will trigger randomly over time.
    /// </summary>
    [CanBeNull] public List<HediffGiver> hediffGivers;

    /// <summary>
    ///     Hediff givers which are triggered when the gene is added. The corresponding hediffs are
    ///     automatically removed when the gene is removed. If the hediff is on a body part, it will
    ///     be removed if the part is lost or replaced with an artificial replacement, and readded if
    ///     the part is regrown.
    /// </summary>
    [CanBeNull] public List<HediffGiver_Event> permanentHediffs;

    /// <summary>
    ///     Hediff givers which are triggered when the pawn is created, either as a new adult or as a
    ///     baby. These are not removed if the gene is lost, and are not added if the gene is added
    ///     to an existing pawn.
    /// </summary>
    [CanBeNull] public List<HediffGiver_Event> congenitalHediffs;

    /// <summary>
    ///     Starting items which have a chance of being generated on the pawn as one of the player's
    ///     starting colonists. A colonist can only have two starting items so these might not be
    ///     added even if the chance is 100%.
    /// </summary>
    [CanBeNull] public List<StartingItemOption> startingItems;

    /// <summary>
    ///     Items which have a chance of being generated as extra apparel on a pawn. This
    ///     includes both player and non-player pawns.
    /// </summary>
    [CanBeNull] public List<StartingItemOption> extraApparel;

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

        if (!permanentHediffs.NullOrEmpty())
        {
            foreach (Tool tool in permanentHediffs.Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
                         .Where(verbGiver => verbGiver != null).SelectMany(verbGiver => verbGiver.tools))
            {
                float armorPenetration = tool.armorPenetration;
                if (armorPenetration < 0f)
                {
                    armorPenetration = tool.power * 0.015f;
                }

                // TODO: Calculate DPS
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "StatsReport_MeleeDamage".Translate(),
                    tool.power.ToStringByStyle(ToStringStyle.FloatTwo), "", 4102);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "ArmorPenetration".Translate(),
                    armorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 4101);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "StatsReport_Cooldown".Translate(),
                    "StatsReport_CooldownFormat".Translate(tool.cooldownTime.ToStringDecimalIfSmall()), "", 4100);
            }
        }

        if (femaleChance != null)
        {
            yield return new(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
                GenderRatioDescription, "XylGenderRatioDesc".TranslateSimple(), 1);
        }
    }

    protected virtual IEnumerable<string> GetCustomEffectDescriptions()
    {
        if (bodySizeFactor != 1.0f)
            yield return $"{"BodySize".Translate().CapitalizeFirst()}: {bodySizeFactor.ToStringPercent()}";
        if (healthScaleFactor != 1.0f)
            yield return $"{"HitPointsBasic".Translate().CapitalizeFirst()}: {healthScaleFactor.ToStringPercent()}";

        if (!permanentHediffs.NullOrEmpty())
        {
            foreach (Tool tool in permanentHediffs.Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
                         .Where(verbGiver => verbGiver != null).SelectMany(verbGiver => verbGiver.tools))
            {
                float armorPenetration = tool.armorPenetration;
                if (armorPenetration < 0f)
                {
                    armorPenetration = tool.power * 0.015f;
                }

                // TODO: Calculate DPS
                yield return $"{"StatsReport_MeleeDamage".Translate()}: {tool.power.ToStringByStyle(ToStringStyle.FloatTwo)}";
                yield return $"{"ArmorPenetration".Translate()}: {armorPenetration.ToStringPercent()}";
                yield return
                    $"{"StatsReport_Cooldown".Translate()}: {"StatsReport_CooldownFormat".Translate(tool.cooldownTime.ToStringDecimalIfSmall())}";
            }
        }

        if (femaleChance != null)
            yield return $"{"XylGenderRatioLabel".TranslateSimple()}: {GenderRatioDescription}";

        if (!addDesignators.NullOrEmpty())
        {
            yield return
                $"{"XylNewBuildings".Translate()}: {addDesignators.Select(def => def.LabelCap.ToString()).OrderBy(s => s).ToCommaList()}";
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
