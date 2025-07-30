using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class CompProperties_PawnRenderProperties : CompProperties
    {
        public CompProperties_PawnRenderProperties()
        {
            compClass = typeof(CompPawn_RenderProperties);
        }
    }

    public class CompPawn_RenderProperties : ThingComp
    {
        public bool hideClothes;
        public bool hideHeadgear;

        public Job job;
    }
}
