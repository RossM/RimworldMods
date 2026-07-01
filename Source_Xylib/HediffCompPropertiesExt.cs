namespace Xylib;

public class HediffCompPropertiesExt : HediffCompProperties
{
    public virtual IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest request)
    {
        return [];
    }
}
