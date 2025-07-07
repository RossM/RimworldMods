using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_Hediff : DefModExtension
    {
        public List<HediffGiver> hediffGivers;
        public bool applyImmediately = false;
        public float mtbDays = 0.0f;
    }

    [UsedImplicitly]
    public class Gene_Hediff : Gene, IGene_HediffSource
    {
        public GeneDefExtension_Hediff DefExt => def.GetModExtension<GeneDefExtension_Hediff>();

        public override void PostAdd()
        {
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
                    pawn.IsHashIntervalTick(60, delta))
                {
                    foreach (var hediffGiver in extension.hediffGivers)
                    {
                        if (Rand.MTBEventOccurs(extension.mtbDays, 60000f, 60f))
                            hediffGiver.TryApply(pawn);
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

        public bool CausesHediff(HediffDef hediffDef)
        {
            return DefExt?.hediffGivers.Any(g => g.hediff == hediffDef) ?? false;
        }
    }
}