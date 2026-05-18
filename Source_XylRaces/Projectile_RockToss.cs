using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedImplicitly]
    public class Projectile_RockToss : Projectile_Explosive
    {
        public override Graphic Graphic => GetComp<CompThingContainer>()?.ContainedThing?.Graphic ?? base.Graphic;
    }
}
