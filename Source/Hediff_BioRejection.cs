using JetBrains.Annotations;
using Verse;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class Hediff_BioRejection : Hediff
    {
        public override float Severity
        {
            get
            {
                using (new ProfileBlock())
                {
                    return pawn.health.hediffSet.CountAddedAndImplantedParts() * 1.0f;
                }
            }
            set { }
        }
    }
}
