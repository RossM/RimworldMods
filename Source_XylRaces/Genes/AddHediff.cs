using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class GeneDefExtension_Hediff : GeneDefExtension
    {
        public List<HediffGiver> hediffGivers;
        public bool applyImmediately = false;
        public bool reapplyOnPartRestored = false;
        public float mtbDays = 0.0f;

        protected override IEnumerable<string> GetCustomEffectDescriptions()
        {
            foreach (Tool tool in hediffGivers.Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
                         .Where(verbGiver => verbGiver != null).SelectMany(verbGiver => verbGiver.tools))
            {
                float armorPenetration = tool.armorPenetration;
                if (armorPenetration < 0f)
                {
                    armorPenetration = tool.power * 0.015f;
                }

                yield return $"{"StatsReport_MeleeDamage".Translate()}: {tool.power.ToStringByStyle(ToStringStyle.FloatTwo)}";
                yield return $"{"ArmorPenetration".Translate()}: {armorPenetration.ToStringPercent()}";
                yield return
                    $"{"StatsReport_Cooldown".Translate()}: {"StatsReport_CooldownFormat".Translate(tool.cooldownTime.ToStringDecimalIfSmall())}";
            }
        }

        protected override IEnumerable<StatDrawEntry> GetSpecialDisplayStats()
        {
            foreach (Tool tool in hediffGivers.Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
                         .Where(verbGiver => verbGiver != null).SelectMany(verbGiver => verbGiver.tools))
            {
                float armorPenetration = tool.armorPenetration;
                if (armorPenetration < 0f)
                {
                    armorPenetration = tool.power * 0.015f;
                }

                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "StatsReport_MeleeDamage".Translate(),
                    tool.power.ToStringByStyle(ToStringStyle.FloatTwo), "", 4102);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "ArmorPenetration".Translate(),
                    armorPenetration.ToStringPercent(), "ArmorPenetrationExplanation".Translate(), 4101);
                yield return new StatDrawEntry(StatCategoryDefOf.Weapon_Melee, "StatsReport_Cooldown".Translate(),
                    "StatsReport_CooldownFormat".Translate(tool.cooldownTime.ToStringDecimalIfSmall()), "", 4100);
            }
        }
    }

    [UsedImplicitly]
    public class AddHediff : Gene, IGene_HediffSource
    {
        public GeneDefExtension_Hediff DefExt => def.GetModExtension<GeneDefExtension_Hediff>();

        const int checkInterval = 60;

        public HashSet<BodyPartRecord> affectedParts;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref affectedParts, nameof(affectedParts));
        }

        public override void PostAdd()
        {
            // Statues don't have a kindDef set, which causes a crash. Check for that and abort.
            if (pawn.kindDef == null)
                return;

            if (Active && DefExt is { hediffGivers: not null, applyImmediately: true })
            {
                foreach (var hediffGiver in DefExt.hediffGivers)
                    Apply(hediffGiver);
            }

            base.PostAdd();
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);

            if (!Active)
                return;
            if (DefExt is not { hediffGivers: not null, mtbDays: > 0.0f })
                return;
            if (!pawn.IsHashIntervalTick(checkInterval, delta))
                return;

            foreach (var hediffGiver in DefExt.hediffGivers)
            {
                if (Rand.MTBEventOccurs(DefExt.mtbDays, GenDate.TicksPerDay, checkInterval))
                    Apply(hediffGiver);
            }
        }

        private void Apply(HediffGiver hediffGiver)
        {
            HashSet<Hediff> oldHediffs = [..pawn.health.hediffSet.hediffs];

            if (!hediffGiver.TryApply(pawn))
                return;

            affectedParts ??= [];
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.Where(hediff => !oldHediffs.Contains(hediff)))
                affectedParts.Add(hediff.Part);
        }

        public void NotifyStateChange()
        {
            if (!DefExt.reapplyOnPartRestored)
                return;

            if (!Active)
                return;
            if (DefExt is not { hediffGivers: not null, reapplyOnPartRestored: true })
                return;
            if (affectedParts.NullOrEmpty())
                return;

            foreach (var hediffGiver in DefExt.hediffGivers)
            {
                if (hediffGiver.partsToAffect.NullOrEmpty())
                    continue;

                List<BodyPartRecord> partsToAdd = [];
                List<BodyPartRecord> partsToRemove = [];
                HediffDef hediffDef = hediffGiver.hediff;

                foreach (BodyPartRecord part in affectedParts)
                {
                    if (!hediffGiver.partsToAffect.Contains(part.def))
                        continue;

                    bool alreadyHasHediff = false;
                    bool missingPart = false;
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
                    {
                        if (hediff.Part != part)
                            continue;

                        if (hediff.def == hediffDef)
                            alreadyHasHediff = true;
                        else if (typeof(Hediff_AddedPart).IsAssignableFrom(hediff.def.hediffClass))
                            missingPart = true;
                        else if (typeof(Hediff_MissingPart).IsAssignableFrom(hediff.def.hediffClass))
                            missingPart = true;
                    }

                    if (missingPart && alreadyHasHediff)
                        partsToRemove.Add(part);
                    else if (!missingPart && !alreadyHasHediff)
                        partsToAdd.Add(part);
                }

                foreach (BodyPartRecord part in partsToAdd)
                {
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, part);
                    pawn.health.AddHediff(hediff);
                }

                foreach (BodyPartRecord part in partsToRemove)
                {
                    Hediff hediff = pawn.health.hediffSet.hediffs.First(h => h.def == hediffDef && h.Part == part);
                    pawn.health.RemoveHediff(hediff);
                }
            }
        }

        public override void PostRemove()
        {
            var extension = DefExt;
            if (Active && extension?.hediffGivers != null)
            {
                HashSet<HediffDef> defsToRemove = [..extension.hediffGivers.Select(hediffGiver => hediffGiver.hediff)];
                foreach (var hediff in pawn.health.hediffSet.hediffs
                             .Where(hediff => defsToRemove.Contains(hediff.def))
                             .ToList())
                    pawn.health.RemoveHediff(hediff);
            }

            base.PostRemove();
        }

        bool IGene_HediffSource.CausesHediff(HediffDef hediffDef)
        {
            return DefExt?.hediffGivers.Any(g => g.hediff == hediffDef) ?? false;
        }
    }
}
