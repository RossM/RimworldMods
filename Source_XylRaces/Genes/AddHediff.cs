using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace XylXenos.Genes
{
    [UsedImplicitly]
    public class AddHediff : GeneExt, IGene_HediffSource, INotificationListener
    {
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

            if (Active && !DefExt.permanentHediffs.NullOrEmpty())
            {
                foreach (var hediffGiver in DefExt.permanentHediffs)
                    Apply(hediffGiver);
            }

            base.PostAdd();
        }

        private void Apply(HediffGiver_Event hediffGiver)
        {
            HashSet<Hediff> oldHediffs = [..pawn.health.hediffSet.hediffs];

            if (!hediffGiver.EventOccurred(pawn))
                return;

            affectedParts ??= [];
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs.Where(hediff => !oldHediffs.Contains(hediff)))
                affectedParts.Add(hediff.Part);
        }

        public void Notify_HediffStateChange()
        {
            if (!Active)
                return;
            if (DefExt.permanentHediffs.NullOrEmpty())
                return;
            if (affectedParts.NullOrEmpty())
                return;

            foreach (var hediffGiver in DefExt.permanentHediffs)
            {
                if (hediffGiver.partsToAffect.NullOrEmpty())
                    continue;

                List<BodyPartRecord> partsToAdd = [];
                List<BodyPartRecord> partsToRemove = [];
                HediffDef hediffDef = hediffGiver.hediff;
                int partCount = 0;

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

        public override void PostRemove()
        {
            var extension = DefExt;
            if (Active && extension?.permanentHediffs != null)
            {
                HashSet<HediffDef> defsToRemove = [..extension.permanentHediffs.Select(hediffGiver => hediffGiver.hediff)];
                foreach (var hediff in pawn.health.hediffSet.hediffs
                             .Where(hediff => defsToRemove.Contains(hediff.def))
                             .ToList())
                    pawn.health.RemoveHediff(hediff);
            }

            base.PostRemove();
        }

        bool IGene_HediffSource.CausesHediff(HediffDef hediffDef)
        {
            return DefExt?.permanentHediffs?.Any(g => g.hediff == hediffDef) ?? false;
        }

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostHediffStateChange, pawn, Notify_HediffStateChange);
        }
    }
}
