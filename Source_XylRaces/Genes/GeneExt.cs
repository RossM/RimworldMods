using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
    public class GeneExt : Gene, INotificationListener
    {
        [NotNull]
        public DefExt DefExt => this.DefExt()!;

        [Unsaved] private bool removing = false;

        public override bool Active
        {
            get
            {
                if (!base.Active)
                    return false;
                if (DefExt.gender != null && DefExt.gender != pawn.gender)
                    return false;
                if (DefExt.geneType == GeneType.Endogene && !pawn.genes.HasEndogene(def))
                    return false;
                if (DefExt.geneType == GeneType.Xenogene && !pawn.genes.HasXenogene(def))
                    return false;
                if (DefExt.allowMutants && pawn.IsMutant)
                    return false;
                return true;
            }
        }

        public virtual IEnumerable<ThingDefCount> GetStartingItems()
        {
            if (DefExt.startingItems.NullOrEmpty())
                yield break;

            foreach (var startingItem in DefExt.startingItems)
            {
                if (!Rand.Chance(startingItem.chance))
                    continue;

                var itemDef = startingItem.item ?? DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(thingDef => Validate(thingDef, startingItem)).RandomElement();

                yield return new(itemDef, Mathf.Clamp(startingItem.count.RandomInRange, 1, itemDef.stackLimit));
            }

            bool Validate(ThingDef thingDef, StartingItemOption startingItem)
            {
                return thingDef.ingestible?.foodType.HasFlag(startingItem.foodType) == true;
            }
        }

        public void RemoveInvalidChemicalHediffs()
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

        public override void PostAdd()
        {
            if (Active && !DefExt.permanentHediffs.NullOrEmpty())
            {
                foreach (var hediffGiver in DefExt.permanentHediffs)
                    hediffGiver.EventOccurred(pawn);
            }

            RemoveInvalidChemicalHediffs();

            if (DefExt.hasPsycast)
                pawn.psychicEntropy.SetInitialPsyfocusLevel();

            base.PostAdd();
        }

        public override void PostRemove()
        {
            removing = true;

            if (!DefExt.permanentHediffs.NullOrEmpty())
            {
                foreach (var hediff in GetLinkedHediffs())
                    pawn.health.RemoveHediff(hediff);
            }

            RemoveInvalidChemicalHediffs();

            base.PostRemove();
        }

        public override void Reset()
        {
            base.Reset();

            if (!DefExt.permanentHediffs.NullOrEmpty())
            {
                foreach (var hediff in GetLinkedHediffs())
                    hediff.Severity = hediff.def.initialSeverity;
            }
        }

        private IEnumerable<Hediff> GetLinkedHediffs()
        {
            if (DefExt.permanentHediffs.NullOrEmpty())
                return Enumerable.Empty<Hediff>();

            HashSet<HediffDef> defs = [.. DefExt.permanentHediffs.Select(hediffGiver => hediffGiver.hediff)];
            return pawn.health.hediffSet.hediffs.Where(hediff => defs.Contains(hediff.def)).ToList();
        }

        public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
        {
            if (!DefExt.permanentHediffs.NullOrEmpty())
            {
                foreach (Tool tool in DefExt.permanentHediffs
                             .Select(hediffGiver => hediffGiver.hediff.CompProps<HediffCompProperties_VerbGiver>())
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

            if (DefExt.femaleChance != null)
            {
                yield return new(StatCategoryDefOf.Genetics, "XylGenderRatioLabel".TranslateSimple(),
                    DefExt.GetGenderRatioDescription(), "XylGenderRatioDesc".TranslateSimple(), 1);
            }
        }

        public void Notify_HediffStateChange()
        {
            if (!Active)
                return;
            if (DefExt.permanentHediffs.NullOrEmpty())
                return;
            if (removing)
                return;

            foreach (var hediffGiver in DefExt.permanentHediffs)
            {
                if (hediffGiver.partsToAffect.NullOrEmpty())
                    continue;

                List<BodyPartRecord> partsToAdd = [];
                List<BodyPartRecord> partsToRemove = [];
                HediffDef hediffDef = hediffGiver.hediff;
                int partCount = 0;

                foreach (BodyPartRecord part in pawn.def.race.body.AllParts)
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

                    if (alreadyHasHediff)
                        partCount++;

                    if (missingPart && alreadyHasHediff)
                        partsToRemove.Add(part);
                    else if (!missingPart && !alreadyHasHediff)
                        partsToAdd.Add(part);
                }

                int maxToAdd = hediffGiver.countToAffect - partCount;
                partsToAdd.Shuffle();

                foreach (BodyPartRecord part in partsToAdd.Take(maxToAdd))
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

        public virtual void RegisterWith(NotificationManager manager)
        {
            if (!DefExt.permanentHediffs.NullOrEmpty())
                manager.Register(NotificationEvent.PostHediffStateChange, pawn, Notify_HediffStateChange);
        }
    }
}
