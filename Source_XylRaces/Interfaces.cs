using Verse;

namespace XylRacesCore;

public interface IStartingItemSource
{
    public ThingDefCount? GetStartingItem();
}

public interface INotifyDamageTaken
{
    void Notify_DamageTaken(DamageInfo damageInfo);
}

public interface INotifyApparelChanged
{
    void Notify_ApparelChanged(Pawn pawn);
}
