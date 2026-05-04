using JetBrains.Annotations;
using RimWorld;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class CompProperties_AbilityToxicBurst : CompProperties_AbilityReleaseGas
    {
        public float AIUseRadius;

        public CompProperties_AbilityToxicBurst()
        {
            compClass = typeof(CompAbilityEffect_ToxicBurst);
        }
    }
}
