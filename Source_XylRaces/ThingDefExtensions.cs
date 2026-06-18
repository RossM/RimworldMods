namespace XylXenos;

public static class ThingDefExtensions
{
    extension(ThingDef thingDef)
    {
        public bool IsRawFoodOrCorpse => thingDef.IsRawHumanFood() || thingDef.IsCorpse;
    }
}
