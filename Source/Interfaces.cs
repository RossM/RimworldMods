using Verse;

namespace XylRacesCore;

public interface IStartingItemGenerator
{
    public ThingDefCount? GetStartingItem();
}