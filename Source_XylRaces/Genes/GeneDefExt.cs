using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
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

    [UsedFromXml]
    public class GeneDefExt : GeneDef
    {
        public IEnumerable<string> CustomEffectDescriptions =>
            customEffectDescriptionsInternal ??= GetCustomEffectDescriptions().ToList();

        public bool showInXenotypeCreation = true;
        public Gender? gender;
        public GeneType? geneType;

        public float bodySizeFactor = 1.0f;
        public float healthScaleFactor = 1.0f;

        public float slaveRebellionMtbFactor = 1.0f;
        public float slaveRebellionThresholdDays = float.MaxValue;

        public float manhunterOnDamageChanceFactor = 1.0f;
        public float manhunterOnTameFailChanceFactor = 1.0f;

        public bool showInDrugPolicies = false;

        // These are triggered randomly over time
        public List<HediffGiver> hediffGivers;

        // These are triggered when the gene is added
        public List<HediffGiver_Event> permanentHediffs;

        // These are triggered when a character with the gene is created
        public List<HediffGiver_Event> congenitalHediffs;

        public List<JoyGiverFactor> joyGiverChanceFactors;
        public List<BuildableDef> addDesignators;
        public List<RenderNodeModifier> renderNodeModifiers;
        public List<FactionDef> disableHostilityFromFactions;
        public List<GeneIngestionThoughtOverride> ingestionThoughtOverrides;

        [NoTranslate] public string extraIconPath;

        public GeneDefExt()
        {
            geneClass = typeof(GeneExt);
        }

        public Texture2D ExtraIcon
        {
            get
            {
                cachedExtraIcon ??= extraIconPath.NullOrEmpty()
                    ? Icon
                    : ContentFinder<Texture2D>.Get(iconPath) ?? Icon;
                return cachedExtraIcon;
            }
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var stat in base.SpecialDisplayStats(req))
                yield return stat;

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
        }

        protected virtual IEnumerable<string> GetCustomEffectDescriptions()
        {
            if (slaveRebellionMtbFactor != 1.0f)
                yield return $"{"SlaveRebellionMTBDays".Translate()} x{slaveRebellionMtbFactor.ToStringPercent()}";

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

            if (!permanentHediffs.NullOrEmpty() && !typeof(AddHediff).IsAssignableFrom(geneClass))
                yield return "permanentHediffs exist but geneClass is not AddHediff or subclass thereof";

            if (geneType != null && !typeof(GeneExt).IsAssignableFrom(geneClass))
                yield return "geneType set but geneClass is not GeneExt or subclass thereof";
            if (gender != null && !typeof(GeneExt).IsAssignableFrom(geneClass))
                yield return "gender set but geneClass is not GeneExt or subclass thereof";
        }

        #region Implementation

        private List<string> customEffectDescriptionsInternal;

        private Texture2D cachedExtraIcon;

        #endregion
    }
}
