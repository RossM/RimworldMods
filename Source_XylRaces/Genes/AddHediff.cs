using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Hediff : GeneDefExtension
    {
        public List<HediffGiver> hediffGivers;
        public bool applyImmediately = false;
        public bool reapplyOnPartRestored = false;
        public float mtbDays = 0.0f;
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

            Scribe_Collections.Look(ref affectedParts, nameof(affectedParts), LookMode.Reference);
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
                if (Rand.MTBEventOccurs(DefExt.mtbDays, 60000f, checkInterval))
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

                List<BodyPartRecord> partsToAdd = new();
                List<BodyPartRecord> partsToRemove = new();
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
