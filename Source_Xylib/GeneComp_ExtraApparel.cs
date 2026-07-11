namespace Xylib;

[PublicAPI]
public class StartingApparelOption
{
    public required ThingDef item;
    public float chance = 1.0f;
    public IntRange count = IntRange.Zero;
    public bool ignoreRestrictions;
}

[UsedFromXml]
[PublicAPI]
public class GeneCompProperties_ExtraApparel : GeneCompProperties
{
    public required List<StartingApparelOption> items;

    public GeneCompProperties_ExtraApparel()
    {
        compClass = typeof(GeneComp_ExtraApparel);
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var error in base.ConfigErrors())
            yield return error;

        if (items is null)
            yield break;

        foreach (var item in items)
        {
            if (item.item is null)
                yield return $"null {nameof(item.item)} in {nameof(item)}";
            if (item.count.IsInvalid)
                yield return $"invalid {nameof(item.count)} in {nameof(item)}";
        }
    }
}

[PublicAPI]
public class GeneComp_ExtraApparel : GeneComp, IEventListener
{
    public GeneCompProperties_ExtraApparel Props => (GeneCompProperties_ExtraApparel)props;

    public void GenerateExtraApparel()
    {
        DebugAssert.NotNull(Pawn.apparel);

        foreach (var item in Props.items)
        {
            if (!ValidApparel(Pawn, item.item, item.ignoreRestrictions))
                continue;
            if (!Rand.Chance(item.chance))
                continue;

            if (PawnApparelGenerator.GenerateApparelOfDefFor(Pawn, item.item) is { } apparel && apparel.PawnCanWear(Pawn))
            {
                PawnApparelGenerator.PostProcessApparel(apparel, Pawn);
                PawnGenerator.PostProcessGeneratedGear(apparel, Pawn);
                Pawn.apparel.Wear(apparel, dropReplacedApparel: false);
            }
        }
    }

    public static bool ValidApparel(Pawn pawn, ThingDef? thing, bool ignoreRestrictions = false)
    {
        if (thing?.apparel?.PawnCanWear(pawn) is not true)
            return false;

        if (ignoreRestrictions)
            return true;

        List<string> apparelTags = thing.apparel.tags ?? [];

        if (pawn.kindDef?.apparelTags is { Count: > 0 } &&
            !pawn.kindDef.apparelTags.Any(apparelTags.Contains))
        {
            return false;
        }

        if (pawn.kindDef?.apparelDisallowTags is { Count: > 0 } &&
            pawn.kindDef.apparelDisallowTags.Any(apparelTags.Contains))
        {
            return false;
        }

        return true;
    }

    public void Notify_PostGenerateNewPawn(PawnGenerationRequest request)
    {
        if (!request.ForceNoGear && !request.AllowedDevelopmentalStages.Newborn())
            GenerateExtraApparel();
    }

    public void Notify_PostRedressPawn(PawnGenerationRequest request)
    {
        if (!request.ForceNoGear && !request.AllowedDevelopmentalStages.Newborn())
            GenerateExtraApparel();
    }

    void IEventListener.RegisterWith(EventManager manager)
    {
        manager.Register<PawnGenerationRequest>(EventDefOf.PostGenerateNewPawn, Pawn, Notify_PostGenerateNewPawn);
        manager.Register<PawnGenerationRequest>(EventDefOf.PostRedressPawn, Pawn, Notify_PostRedressPawn);
    }

    void IEventListener.PreUnregister(EventManager manager)
    {
    }
}
