using System.Collections.Generic;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class Hediff_BioRejection : HediffWithComps
    {
        public override bool ShouldRemove => false;

        public override float Severity
        {
            get => pawn.health.hediffSet.CountAddedAndImplantedParts() * 1.0f;
            set { }
        }

        public override string Description
        {
            get
            {
                List<string> causes = [];

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                foreach (Hediff hediff in hediffs)
                {
                    if (hediff.def.countsAsAddedPartOrImplant)
                    {
                        causes.Add(hediff.Label);
                    }
                }

                return $"""
                    {base.Description}
                    
                    {"CausedBy".Translate()}: {causes.ToCommaList().CapitalizeFirst()}
                    """;
            }
        }
    }
}
