using System.Collections.Generic;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
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

    [UsedFromXml]
    public class GeneDefExt : GeneDef
    {
        public IEnumerable<string> CustomEffectDescriptions =>
            customEffectDescriptionsInternal ??= GetCustomEffectDescriptions().ToList();

        public Gender? gender;

        public float bodySizeFactor = 1.0f;
        public float healthScaleFactor = 1.0f;

        public bool showInDrugPolicies = false;

        // These are triggered randomly over time
        public List<HediffGiver> hediffGivers;

        // These are triggered when the gene is added
        public List<HediffGiver_Event> permanentHediffs;

        // These are triggered when a character with the gene is created
        public List<HediffGiver_Event> congenitalHediffs;

        public List<JoyGiverFactor> joyGiverChanceFactors;

        private List<string> customEffectDescriptionsInternal;

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
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var configError in base.ConfigErrors())
                yield return configError;

            if (!permanentHediffs.NullOrEmpty() && !typeof(AddHediff).IsAssignableFrom(geneClass))
                yield return "permanentHediffs exist but geneClass is not AddHediff or subclass thereof";
        }
    }
}
