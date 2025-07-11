using Verse;

namespace XylRacesCore;

public interface IStartingItemSource
{
    public ThingDefCount? GetStartingItem();
}

public interface INotifyDamageTaken
{
    void Notify_DamageTaken(DamageInfo damageInfo, DamageWorker.DamageResult damageResult);
}

public interface INotifyPawnDamagedThing
{
    void Notify_PawnDamagedThing(Thing thing, DamageInfo damageInfo, DamageWorker.DamageResult DamageResult);
}

