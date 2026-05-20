using RimWorld;
using Verse;

namespace XylXenos
{
    [UsedFromXml]
    public class Projectile_RockToss : Projectile_Explosive
    {
        public override Graphic Graphic => GetComp<CompThingContainer>()?.ContainedThing?.Graphic ?? base.Graphic;
    }
}
