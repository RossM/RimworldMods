using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class Hediff_BioRejection : Hediff
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
                List<string> causes = new();

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                foreach (Hediff hediff in hediffs)
                {
                    if (hediff.def.countsAsAddedPartOrImplant)
                    {
                        causes.Add(hediff.Label);
                    }
                }

                return base.Description + "\n\n" + "CausedBy".Translate() + ": " + causes.ToCommaList().CapitalizeFirst();
            }
        }
    }
}
