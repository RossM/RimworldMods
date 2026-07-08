namespace Xylib;

[PublicAPI]
public class StartingItemOption
{
    public ThingDef? item;
    public FoodTypeFlags foodType;
    public float chance = 1.0f;
    public IntRange count = IntRange.Zero;
    public FloatRange nutritionAmount = FloatRange.Zero;
}

[UsedFromXml]
[PublicAPI]
public class GeneCompProperties_StartingItems : GeneCompProperties
{
    public List<StartingItemOption>? items;

    public GeneCompProperties_StartingItems()
    {
        compClass = typeof(GeneComp_StartingItems);
    }

    public override IEnumerable<string> ConfigErrors()
    {
        if (items is null)
        {
            yield return $"{nameof(items)} is null";
            yield break;
        }

        foreach (var item in items)
        {
            if (item.item is null)
                yield return $"null {nameof(item.item)} in {nameof(items)}";
            if (item.count.IsInvalid)
                yield return $"invalid {nameof(item.count)} in {nameof(items)}";
        }
    }
}

[PublicAPI]
public class GeneComp_StartingItems : GeneComp, IEventListener
{
    public GeneCompProperties_StartingItems Props => (GeneCompProperties_StartingItems)props;

    public virtual IEnumerable<ThingDefCount> GetStartingItems()
    {
        if (Props.items is not { Count: > 0 })
            yield break;

        foreach (var startingItem in Props.items!)
        {
            if (!Rand.Chance(startingItem.chance))
                continue;

            var itemDef = startingItem.item ?? DefDatabase<ThingDef>.AllDefsListForReading
                .Where(thingDef => Validate(thingDef, startingItem)).RandomElement();
            if (itemDef == null)
                continue;

            var itemNutrition = itemDef.GetStatValueAbstract(StatDefOf.Nutrition);
            int count;
            if (startingItem.nutritionAmount != FloatRange.Zero && itemNutrition > 0)
                count = GenMath.RoundRandom(startingItem.nutritionAmount.RandomInRange / itemNutrition);
            else if (startingItem.count != IntRange.Zero)
                count = startingItem.count.RandomInRange;
            else if (itemDef.possessionCount > 0)
                count = itemDef.possessionCount;
            else
                count = 1;

            yield return new(itemDef, Mathf.Clamp(count, 1, itemDef.stackLimit));
        }

        static bool Validate(ThingDef thingDef, StartingItemOption startingItem)
        {
            return thingDef.ingestible?.foodType.HasFlag(startingItem.foodType) is true;
        }
    }

    public void Notify_InGeneratePossessions(List<ThingDefCount> items)
    {
        if (items.Count >= 2)
            return;

        foreach (var item in GetStartingItems())
        {
            items.Add(item);
            if (items.Count >= 2)
                return;
        }
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register<List<ThingDefCount>>(EventDefOf.InGeneratePossessions, Pawn, Notify_InGeneratePossessions);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
