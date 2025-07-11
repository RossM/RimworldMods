using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class Hediff_InnateWeapon : HediffWithComps
    {
        public float GetMeleeDPSValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            var comp = GetComp<HediffComp_VerbGiver>();

            var verbs = VerbUtility.GetAllVerbProperties(comp.VerbProperties, comp.Tools)
                .Where(x => x.verbProps.IsMeleeAttack).ToList();

            float damage = verbs.AverageWeighted(SelectionWeight, x => x.verbProps.AdjustedMeleeDamageAmount(x.tool, pawn, req.Thing, comp));
            float cooldown = verbs.AverageWeighted(SelectionWeight, x => x.verbProps.AdjustedCooldown(x.tool, pawn, req.Thing));
            if (cooldown == 0f)
            {
                return 0f;
            }
            return damage / cooldown;

            float SelectionWeight(VerbUtility.VerbPropertiesWithSource x)
            {
                return x.verbProps.AdjustedMeleeSelectionWeight(x.tool, pawn, req.Thing, null,
                    comesFromPawnNativeVerbs: false);
            }
        }

        public string GetMeleeDPSExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            var verbGiver = def.CompProps<HediffCompProperties_VerbGiver>();
            var verbs = verbGiver.verbs;
            var tools = verbGiver.tools;
            
            Pawn currentWeaponUser = pawn;
            IEnumerable<VerbUtility.VerbPropertiesWithSource> enumerable = from x in VerbUtility.GetAllVerbProperties(verbs, tools)
                where x.verbProps.IsMeleeAttack
                select x;
            var stringBuilder = new StringBuilder();
            foreach (VerbUtility.VerbPropertiesWithSource item in enumerable)
            {
                float damage = item.verbProps.AdjustedMeleeDamageAmount(item.tool, currentWeaponUser, req.Thing, null);
                float cooldown = item.verbProps.AdjustedCooldown(item.tool, currentWeaponUser, req.Thing);
                stringBuilder.AppendLine(item.tool != null
                    ? $"  {item.tool.LabelCap} ({item.ToolCapacity.label})"
                    : $"  {"StatsReport_NonToolAttack".Translate()}:");
                stringBuilder.AppendLine($"    {damage:F1} {"DamageLower".Translate()}");
                stringBuilder.AppendLine($"    {cooldown:F2} {"SecondsPerAttackLower".Translate()}");
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
