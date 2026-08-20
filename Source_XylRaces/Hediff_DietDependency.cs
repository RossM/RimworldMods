namespace XylXenos;
// TODO: Need to handle satisfying the dependency for pawns in caravans. See Caravan_NeedsTracker.TrySatisfyChemicalNeed
// and CaravanInventoryUtility.TryGetBestFood.

[UsedFromXml]
public class DefModExtension_Hediff_DietDependency : DefModExtension
{
    public List<FoodGroupDef>? foodGroups;
    public bool rawOnly = false;
    public float severityReductionPerNutrition = 1f;
    [MustTranslate] public string? foodLabel;

    private HediffDef? parent;

    public override void ResolveReferences(Def parentDef)
    {
        parent = parentDef as HediffDef;
    }

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (var configError in base.ConfigErrors())
            yield return configError;

        if (parent is null)
        {
            yield return $"{nameof(DefModExtension_Hediff_DietDependency)} can only be applied to a {nameof(HediffDef)}";
            yield break;
        }

        if (parent is not { stages.Count: >= Hediff_DietDependency.StageCount })
            yield return $"Must have at least {Hediff_DietDependency.StageCount} stages";
    }
}

[UsedFromXml]
public class Hediff_DietDependency : HediffWithComps, IEventListener
{
    private enum Stages
    {
        // ReSharper disable UnusedMember.Local
        Satisfied = 1,
        Craving = 2,
        MildDeficiency = 3,
        SevereDeficiency = 4,
        Coma = 5,
        // ReSharper restore UnusedMember.Local
    }

    public DefModExtension_Hediff_DietDependency DefExt => field ??= def.GetModExtension<DefModExtension_Hediff_DietDependency>()!;

    public bool ShouldSatisfy => CurStageIndex >= (int)Stages.Craving;

    public override bool ShouldRemove => false;

    public const int StageCount = (int)Stages.Coma + 1;

    public override string? TipStringExtra
    {
        get
        {
            DebugAssert.NotNull(def.stages);
            DebugAssert.True(def.stages.Count >= StageCount);

            string? text = base.TipStringExtra;

            HediffComp_SeverityPerDay? comp_severityPerDay = GetComp<HediffComp_SeverityPerDay>();
            if (comp_severityPerDay is null)
                return text;

            if (!text.NullOrEmpty())
                text += "\n\n";

            var severityPerDay =
                ((HediffCompProperties_SeverityPerDay)comp_severityPerDay.props)
                .severityPerDay;
            var deficiencyDays = def.stages[(int)Stages.MildDeficiency]!.minSeverity / severityPerDay;
            var comaDays = def.stages[(int)Stages.Coma]!.minSeverity / severityPerDay;
            var deathDays = def.lethalSeverity / severityPerDay;
            text += "GeneDefChemicalNeedDurationDesc".Translate(DefExt.foodLabel,
                pawn.Named("PAWN"),
                // ReSharper disable StringLiteralTypo
                "PeriodDays".Translate(deficiencyDays).Named("DEFICIENCYDURATION"),
                "PeriodDays".Translate(comaDays).Named("COMADURATION"),
                "PeriodDays".Translate(deathDays).Named("DEATHDURATION")).Resolve();
            // ReSharper restore StringLiteralTypo
            float daysBehind = Severity / severityPerDay;
            float nutritionPerDay = severityPerDay * DefExt.severityReductionPerNutrition;
            text += "\n\n" + "XylIngestedBehind".Translate(DefExt.foodLabel,
                pawn.Named("PAWN"),
                nutritionPerDay.ToStringDecimalIfSmall().Named("NUTRITION"),
                "PeriodDays".Translate(daysBehind).Named("DURATION"));

            return text;
        }
    }

    public Thing? FindFoodFor(Pawn pawnGettingFood)
    {
        DebugAssert.NotNull(pawnGettingFood.Map);

        ThingOwner<Thing> innerContainer = pawnGettingFood.inventory.innerContainer;
        foreach (Thing item in innerContainer)
        {
            if (FoodValidator(pawnGettingFood, this, item))
                return item;
        }

        if (GenClosest.ClosestThingReachable(pawnGettingFood.Position, pawnGettingFood.Map,
                ThingRequest.ForGroup(ThingRequestGroup.FoodSource),
                PathEndMode.ClosestTouch, TraverseParms.For(pawnGettingFood), 9999f, x => FoodValidator(pawnGettingFood, this, x)) is
            { } thing)
            return thing;

        if (!pawnGettingFood.IsColonist || pawnGettingFood.Map == null)
            return null;

        foreach (Pawn spawnedColonyAnimal in pawnGettingFood.Map.mapPawns.SpawnedColonyAnimals)
        {
            foreach (Thing item in spawnedColonyAnimal.inventory.innerContainer)
            {
                if (FoodValidator(pawnGettingFood, this, item) && !spawnedColonyAnimal.IsForbidden(pawnGettingFood)
                                                               && pawnGettingFood.CanReach(spawnedColonyAnimal, PathEndMode.OnCell,
                                                                   Danger.Some))
                    return item;
            }
        }

        return null;
    }

    private static bool FoodValidator(Pawn pawn, Hediff_DietDependency hediff, Thing food)
    {
        if (!food.def.IsIngestible)
            return false;
        if (food.IsForbidden(pawn))
            return false;
        if (!pawn.CanReserve(food))
            return false;

        return hediff.ValidateFood(food);
    }

    public float NutritionWantedToSatisfy()
    {
        return Severity / DefExt.severityReductionPerNutrition;
    }

    public int ItemsWantedToSatisfy(Thing foodSource, ThingDef foodDef)
    {
        DebugAssert.NotNull(pawn);

        var nutritionNeeded = NutritionWantedToSatisfy();
        var nutritionPerItem = FoodUtility.GetNutrition(pawn, foodSource, foodDef);
        if (nutritionPerItem == 0)
            return 0;
        return Mathf.CeilToInt(nutritionNeeded / nutritionPerItem);
    }

    public bool ValidateFood(Thing food)
    {
        DebugAssert.NotNull(pawn);

        if (food.Destroyed || !food.IngestibleNow)
            return false;

        float nutrition = FoodUtility.NutritionForEater(pawn, food);
        if (nutrition <= 0.0f)
            return false;

        if (!food.def.IsRawFoodOrCorpse && DefExt.rawOnly)
            return false;

        if (ValidFoodType(food.def))
            return true;

        var compIngredients = food.TryGetComp<CompIngredients>();
        if (compIngredients == null)
            return false;
        if (Enumerable.Any(compIngredients.ingredients, ValidFoodType))
            return true;

        return false;
    }

    private bool ValidFoodType(ThingDef ingredient) =>
        DefExt.foodGroups is not { Count: > 0 } || ingredient.FoodGroups.Intersect(DefExt.foodGroups).Any();

    public override void Notify_IngestedThing(Thing food, int numTaken)
    {
        DebugAssert.NotNull(pawn);

        float nutrition = FoodUtility.NutritionForEater(pawn, food);

        if (numTaken > 0)
            nutrition *= numTaken;
        else if (pawn.needs.food?.NutritionWanted != null)
            // If only part of a corpse was consumed, numTaken will be 0, so assume the pawn eats until full.
            // There doesn't seem to be an easy way to get the nutrition gained directly.
            nutrition = Math.Min(nutrition, pawn.needs.food.NutritionWanted);

        if (ValidateFood(food))
            Severity -= nutrition * DefExt.severityReductionPerNutrition;
    }

    public void Notify_PostSatisfyGenes()
    {
        Severity = 0;
    }

    public void RegisterWith(EventManager manager)
    {
        manager.Register(EventDefOf.PostSatisfyChemicalGenes, pawn, Notify_PostSatisfyGenes);
    }

    public void PreUnregister(EventManager manager) { }
}
