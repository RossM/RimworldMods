using RimWorld;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace XylXenos.Genes
{
    public class GeneExt : Gene, IGene_HediffSource, INotificationListener
    {
        public HashSet<BodyPartRecord> partsWithPermanentHediffs;

        [NotNull] public GeneDefExt DefExt => this.DefExt()!;

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
                yield return new(startingItem.item, Mathf.Clamp(startingItem.count.RandomInRange, 1, startingItem.item.stackLimit));
            }
        }

        public void RemoveInvalidHediffs()
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

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref partsWithPermanentHediffs, nameof(partsWithPermanentHediffs));
        }

        public override void PostAdd()
        {
            if (Active && !DefExt.permanentHediffs.NullOrEmpty())
            {
                HashSet<Hediff> oldHediffs = [.. pawn.health.hediffSet.hediffs];

                bool success = false;
                foreach (var hediffGiver in DefExt.permanentHediffs)
                {
                    if (!hediffGiver.EventOccurred(pawn))
                    {
                        success = true;
                    }
                }

                if (success)
                {
                    partsWithPermanentHediffs ??= [];
                    foreach (Hediff hediff in pawn.health.hediffSet.hediffs.Where(hediff => !oldHediffs.Contains(hediff)))
                        partsWithPermanentHediffs.Add(hediff.Part);
                }
            }

            RemoveInvalidHediffs();

            base.PostAdd();
        }

        public override void PostRemove()
        {
            if (Active && DefExt.permanentHediffs != null)
            {
                HashSet<HediffDef> defsToRemove = [.. DefExt.permanentHediffs.Select(hediffGiver => hediffGiver.hediff)];
                foreach (var hediff in pawn.health.hediffSet.hediffs
                             .Where(hediff => defsToRemove.Contains(hediff.def))
                             .ToList())
                    pawn.health.RemoveHediff(hediff);
            }

            RemoveInvalidHediffs();

            base.PostRemove();
        }

        public void Notify_HediffStateChange()
        {
            if (!Active)
                return;
            if (DefExt.permanentHediffs.NullOrEmpty())
                return;
            if (partsWithPermanentHediffs.NullOrEmpty())
                return;

            foreach (var hediffGiver in DefExt.permanentHediffs)
            {
                if (hediffGiver.partsToAffect.NullOrEmpty())
                    continue;

                List<BodyPartRecord> partsToAdd = [];
                List<BodyPartRecord> partsToRemove = [];
                HediffDef hediffDef = hediffGiver.hediff;
                int partCount = 0;

                foreach (BodyPartRecord part in partsWithPermanentHediffs)
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

        bool IGene_HediffSource.CausesHediff(HediffDef hediffDef)
        {
            return DefExt.permanentHediffs?.Any(g => g.hediff == hediffDef) ?? false;
        }

        public virtual void RegisterWith(NotificationManager manager)
        {
            if (!DefExt.permanentHediffs.NullOrEmpty())
                manager.Register(NotificationEvent.PostHediffStateChange, pawn, Notify_HediffStateChange);
        }
    }
}
