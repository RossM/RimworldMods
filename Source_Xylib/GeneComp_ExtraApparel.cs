namespace Xylib;

public class StartingApparelOption
{
    public ThingDef item;
    public float chance = 1.0f;
    public IntRange count = IntRange.Zero;
    public bool ignoreRestrictions;
}

[UsedFromXml]
public class GeneCompProperties_ExtraApparel : GeneCompProperties
{
    public List<StartingApparelOption> items;

    public GeneCompProperties_ExtraApparel()
    {
        compClass = typeof(GeneComp_ExtraApparel);
    }
}

public class GeneComp_ExtraApparel : GeneComp, IEventListener
{
    public GeneCompProperties_ExtraApparel Props => (GeneCompProperties_ExtraApparel)props;

    public void GenerateExtraApparel()
    {
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

    public static bool ValidApparel(Pawn pawn, ThingDef thing, bool ignoreRestrictions = false)
    {
        if (thing == null)
            return false;

        if (!thing.apparel.PawnCanWear(pawn))
            return false;

        if (ignoreRestrictions)
            return true;

        if (!pawn.kindDef.apparelTags.NullOrEmpty() &&
            !pawn.kindDef.apparelTags.Any(tag => thing.apparel.tags.Contains(tag)))
        {
            return false;
        }

        if (!pawn.kindDef.apparelDisallowTags.NullOrEmpty() &&
            pawn.kindDef.apparelDisallowTags.Any(tag => thing.apparel.tags.Contains(tag)))
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
