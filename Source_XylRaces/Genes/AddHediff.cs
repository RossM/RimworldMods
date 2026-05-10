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

        public override void PostAdd()
        {
            // Statues don't have a kindDef set, which causes a crash. Check for that and abort.
            if (pawn.kindDef == null)
                return;

            var extension = DefExt;
            if (Active && extension is { hediffGivers: not null, applyImmediately: true })
            {
                foreach (var hediffGiver in extension.hediffGivers)
                    hediffGiver.TryApply(pawn);
            }

            base.PostAdd();
        }

        public override void TickInterval(int delta)
        {
            using (new ProfileBlock())
            {
                base.TickInterval(delta);

                var extension = DefExt;
                if (Active && extension is { hediffGivers: not null, mtbDays: > 0.0f } &&
                    pawn.IsHashIntervalTick(checkInterval, delta))
                {
                    foreach (var hediffGiver in extension.hediffGivers)
                    {
                        if (Rand.MTBEventOccurs(extension.mtbDays, 60000f, checkInterval))
                            hediffGiver.TryApply(pawn);
                    }
                }
            }
        }

        public void NotifyStateChange()
        {
            if (!DefExt.reapplyOnPartRestored)
                return;

            using (new ProfileBlock())
            {
                var extension = DefExt;
                if (Active && extension is { hediffGivers: not null, reapplyOnPartRestored: true })
                {
                    foreach (var hediffGiver in extension.hediffGivers)
                    {
                        if (hediffGiver.partsToAffect.NullOrEmpty())
                            continue;

                        List<BodyPartRecord> partsToAdd = new();
                        List<BodyPartRecord> partsToRemove = new();
                        HediffDef hediffDef = hediffGiver.hediff;

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
                                else if (hediff.def.hediffClass == typeof(Hediff_AddedPart))
                                    missingPart = true;
                                else if (hediff.def.hediffClass == typeof(Hediff_MissingPart))
                                    missingPart = true;
                            }

                            //Log.Message($"Genes.AddHediff.NotifyStateChange: hediffDef={hediffDef} part={part} alreadyHasHediff={alreadyHasHediff} missingPart={missingPart}");
                            if (missingPart && alreadyHasHediff)
                                partsToRemove.Add(part);
                            else if (!missingPart && !alreadyHasHediff)
                                partsToAdd.Add(part);
                        }

                        foreach (BodyPartRecord part in partsToAdd)
                        {
                            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, part);
                            //Log.Message($"Genes.AddHediff.NotifyStateChange: Adding hediff: {hediff}");
                            pawn.health.AddHediff(hediff);
                        }

                        foreach (BodyPartRecord part in partsToRemove)
                        {
                            Hediff hediff = pawn.health.hediffSet.hediffs.First(h => h.def == hediffDef && h.Part == part);
                            //Log.Message($"Genes.AddHediff.NotifyStateChange: Removing hediff: {hediff}");
                            pawn.health.RemoveHediff(hediff);
                        }
                    }
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