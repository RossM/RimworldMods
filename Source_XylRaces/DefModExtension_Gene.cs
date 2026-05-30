using System.Xml;
using XylXenos;

namespace XylXenos;

public class DietDependencyInfo
{
    public FoodKind foodKind = FoodKind.Any;
    public bool rawOnly = false;
    public float severityReductionPerNutrition = 1f;
    [MustTranslate] public string foodLabel;
}

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
    public Type renderNodeClass = null;
    public bool onlyRoot = false;
    public float scale = 1.0f;
    public Vector3 offset = Vector3.zero;

    public bool Matches(PawnRenderNode node)
    {
        if (onlyRoot && node.parent != null)
            return false;
        if (renderNodeClass != null && node.Worker.GetType() != renderNodeClass)
            return false;
        return true;
    }
}

public class StartingItemOption
{
    public ThingDef item;
    public FoodTypeFlags foodType;
    public float chance = 1.0f;
    public IntRange count = IntRange.One;
    public FloatRange nutritionAmount = FloatRange.Zero;
}

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

    public bool showInXenotypeCreation = true;
    public Gender? gender;
    public GeneType? geneType;
    public bool allowMutants = true;

    public int xenotypeStrength;

    public float bodySizeFactor = 1.0f;
    public float healthScaleFactor = 1.0f;

    public float slaveRebellionThresholdDays = float.MaxValue;

    public float manhunterOnDamageChanceFactor = 1.0f;
    public float manhunterOnTameFailChanceFactor = 1.0f;

    public float? femaleChance;

    public bool showInDrugPolicies = false;

    // These are triggered randomly over time
    [CanBeNull] public List<HediffGiver> hediffGivers;

    // These are triggered when the gene is added
    [CanBeNull] public List<HediffGiver_Event> permanentHediffs;

    // These are triggered when a character with the gene is created
    [CanBeNull] public List<HediffGiver_Event> congenitalHediffs;

    [CanBeNull] public List<JoyGiverFactor> joyGiverChanceFactors;
    [CanBeNull] public List<BuildableDef> addDesignators;
    [CanBeNull] public List<RenderNodeModifier> renderNodeModifiers;
    [CanBeNull] public List<FactionDef> disableHostilityFromFactions;
    [CanBeNull] public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;
    [CanBeNull] public List<StartingItemOption> startingItems;

    [NoTranslate] [CanBeNull] public string extraIconPath;

    [CanBeNull] public BonusGenesInfo bonusGenes;
    [CanBeNull] public DietDependencyInfo dietDependency;
    [CanBeNull] public FlightInfo flight;
    [CanBeNull] public HyperlactationInfo hyperlactation;
    [CanBeNull] public SeeingRedInfo seeingRed;

    public bool hasPsycast;

    [CanBeNull] public Def parent;

    public IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
    {
        if (bodySizeFactor != 1.0f)
        {
            yield return new(StatCategoryDefOf.BasicsPawn, "BodySize".Translate(), bodySizeFactor.ToStringPercent(), "Stat_Race_BodySize_Desc".Translate(), 4195);
        }
        if (healthScaleFactor != 1.0f)
        {
            yield return new(StatCategoryDefOf.BasicsPawn, "HitPointsBasic".Translate(), healthScaleFactor.ToStringPercent(), "XylHitPointsDesc".Translate(), 4194);
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
        var geneClass = (parent as GeneDef)?.geneClass ?? (parent as GeneTemplateDef)?.geneClass;
        if (geneClass == null)
            yield break;

        foreach (var configError in base.ConfigErrors())
            yield return configError;

        if (!typeof(GeneExt).IsAssignableFrom(geneClass))
            yield return "geneClass is not GeneExt or subclass thereof";

        if (bonusGenes != null && !typeof(Gene_BonusGenes).IsAssignableFrom(geneClass))
            yield return "bonusGenes set but geneClass is not BonusGene or subclass thereof";
        if (bonusGenes == null && typeof(Gene_BonusGenes).IsAssignableFrom(geneClass))
            yield return "bonusGenes not set but geneClass is BonusGene or subclass thereof";
        if (hyperlactation != null && !typeof(Gene_Hyperlactation).IsAssignableFrom(geneClass))
            yield return "hyperlactation set but geneClass is not Hyperlactation or subclass thereof";
        if (hyperlactation == null && typeof(Gene_Hyperlactation).IsAssignableFrom(geneClass))
            yield return "hyperlactation not set but geneClass is Hyperlactation or subclass thereof";
        if (seeingRed != null && !typeof(Gene_SeeingRed).IsAssignableFrom(geneClass))
            yield return "seeingRed set but geneClass is not SeeingRed or subclass thereof";
        if (seeingRed == null && typeof(Gene_SeeingRed).IsAssignableFrom(geneClass))
            yield return "seeingRed not set but geneClass is SeeingRed or subclass thereof";
    }

    public override void ResolveReferences(Def parentDef)
    {
        base.ResolveReferences(parentDef);

        parent = parentDef;

        GeneDefExtensions.defExtCache.Clear();

        switch (parentDef)
        {
            case GeneDef geneDef:
            {
                if (geneDef.geneClass == typeof(Gene))
                    geneDef.geneClass = typeof(GeneExt);
                break;
            }
            case GeneTemplateDef templateDef:
            {
                if (templateDef.geneClass == typeof(Gene))
                    templateDef.geneClass = typeof(GeneExt);
                break;
            }
            default:
            {
                Log.Warning(
                    $"XylXenos DefExt is applied to def other than GeneDef or GeneTemplateDef: {parentDef.GetType().Name} {parentDef.defName}");
                break;
            }
        }
    }
}