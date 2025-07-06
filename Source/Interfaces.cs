using Verse;

namespace XylRacesCore;

public interface IStartingItemSource
{
    public ThingDefCount? GetStartingItem();
}