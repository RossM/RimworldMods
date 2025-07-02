using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    // ReSharper disable once UnusedMember.Global
    public class Hediff_Weapon : HediffWithComps
    {
        public float GetMeleeDPSValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var verbGiver = def.CompProps<HediffCompProperties_VerbGiver>();
            var verbs = verbGiver.verbs;
            var tools = verbGiver.tools;
            ;
            Pawn attacker = pawn;
            float damage = (from x in VerbUtility.GetAllVerbProperties(verbs, tools)
                         where x.verbProps.IsMeleeAttack
                         select x).AverageWeighted((VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, attacker, req.Thing, null, comesFromPawnNativeVerbs: false), (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeDamageAmount(x.tool, attacker, req.Thing, null));
            float cooldown = (from x in VerbUtility.GetAllVerbProperties(verbs, tools)
                          where x.verbProps.IsMeleeAttack
                          select x).AverageWeighted((VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedMeleeSelectionWeight(x.tool, attacker, req.Thing, null, comesFromPawnNativeVerbs: false), (VerbUtility.VerbPropertiesWithSource x) => x.verbProps.AdjustedCooldown(x.tool, attacker, req.Thing));
            if (cooldown == 0f)
            {
                return 0f;
            }
            return damage / cooldown;
        }

        public string GetMeleeDPSExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var verbGiver = def.CompProps<HediffCompProperties_VerbGiver>();
            var verbs = verbGiver.verbs;
            var tools = verbGiver.tools;
            ;
            Pawn currentWeaponUser = pawn;
            IEnumerable<VerbUtility.VerbPropertiesWithSource> enumerable = from x in VerbUtility.GetAllVerbProperties(verbs, tools)
                where x.verbProps.IsMeleeAttack
                select x;
            var stringBuilder = new StringBuilder();
            foreach (VerbUtility.VerbPropertiesWithSource item in enumerable)
            {
                float damage = item.verbProps.AdjustedMeleeDamageAmount(item.tool, currentWeaponUser, req.Thing, null);
                float cooldown = item.verbProps.AdjustedCooldown(item.tool, currentWeaponUser, req.Thing);
                if (item.tool != null)
                {
                    stringBuilder.AppendLine(string.Format("  {0} ({1})", item.tool.LabelCap, item.ToolCapacity.label));
                }
                else
                {
                    stringBuilder.AppendLine(string.Format("  {0}:", "StatsReport_NonToolAttack".Translate()));
                }
                stringBuilder.AppendLine(string.Format("    {0:F1} {1}", damage, "DamageLower".Translate()));
                stringBuilder.AppendLine(string.Format("    {0:F2} {1}", cooldown, "SecondsPerAttackLower".Translate()));
            }
            return stringBuilder.ToString();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (var statDrawEntry in base.SpecialDisplayStats(req))
                yield return statDrawEntry;

            var extraLabelPart = "";

            var hediffCompProperties_VerbGiver = def.CompProps<HediffCompProperties_VerbGiver>();
            if (hediffCompProperties_VerbGiver != null && !hediffCompProperties_VerbGiver.tools.NullOrEmpty())
            {
                Tool tool = hediffCompProperties_VerbGiver.tools[0];
                if (ThingUtility.PrimaryMeleeWeaponDamageType(hediffCompProperties_VerbGiver.tools).armorCategory != null)
                {
                    float armorPenetration = tool.armorPenetration;
                    if (armorPenetration < 0f)
                    {
                        armorPenetration = tool.power * 0.015f;
                    }

                    float meleeDPS = GetMeleeDPSValueUnfinalized(req);
                    string meleeDPSExplanation = GetMeleeDPSExplanationUnfinalized(req, ToStringNumberSense.Absolute);
                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee,
                        StatDefOf.MeleeDPS.LabelForFullStatList, meleeDPS.ToStringByStyle(ToStringStyle.FloatTwo), meleeDPSExplanation, 5100);
                    yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "ArmorPenetration".Translate() + extraLabelPart, armorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 4100);
                }
            }
        }
    }
}
