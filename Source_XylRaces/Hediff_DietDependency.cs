using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using XylXenos.Genes;

namespace XylXenos
{
    // TODO: Need to handle satisfying the dependency for pawns in caravans. See Caravan_NeedsTracker.TrySatisfyChemicalNeed
    // and CaravanInventoryUtility.TryGetBestFood.

    [UsedFromXml]
    public class Hediff_DietDependency : HediffWithComps, INotificationListener
    {
        enum Stages
        {
            // ReSharper disable UnusedMember.Local
            Satisfied = 1,
            Craving = 2,
            MildDeficiency = 3,
            SevereDeficiency = 4,
            Coma = 5,
            // ReSharper restore UnusedMember.Local
        }

        public GeneExt Gene => geneInternal ??= (GeneExt)pawn.genes.GetGene(GetComp<HediffComp_Genetic>().Props.gene);

        public bool ShouldSatisfy => Severity >= def.stages[(int)Stages.Craving].minSeverity;

        public float SeverityReductionPerNutrition => Gene.DefExt.dietDependency!.severityReductionPerNutrition;
        private string FoodLabel => Gene.DefExt.dietDependency!.foodLabel;
        private FoodKind FoodKind => Gene.DefExt.dietDependency!.foodKind;
        private bool RawOnly => Gene.DefExt.dietDependency!.rawOnly;

        private GeneExt geneInternal;

        public override string TipStringExtra
        {
            get
            {
                string text = base.TipStringExtra;

                if (Gene != null)
                {
                    if (!text.NullOrEmpty())
                        text += "\n\n";

                    var severityPerDay =
                        ((HediffCompProperties_SeverityPerDay)GetComp<HediffComp_SeverityPerDay>().props)
                        .severityPerDay;
                    var deficiencyDays = def.stages[(int)Stages.MildDeficiency].minSeverity / severityPerDay;
                    var comaDays = def.stages[(int)Stages.Coma].minSeverity / severityPerDay;
                    var deathDays = def.lethalSeverity / severityPerDay;
                    text += "GeneDefChemicalNeedDurationDesc".Translate(FoodLabel,
                        pawn.Named("PAWN"),
                        // ReSharper disable StringLiteralTypo
                        "PeriodDays".Translate(deficiencyDays).Named("DEFICIENCYDURATION"),
                        "PeriodDays".Translate(comaDays).Named("COMADURATION"),
                        "PeriodDays".Translate(deathDays).Named("DEATHDURATION")).Resolve();
                    // ReSharper restore StringLiteralTypo
                    float daysBehind = Severity / severityPerDay;
                    float nutritionPerDay = severityPerDay * SeverityReductionPerNutrition;
                    text += "\n\n" + "XylIngestedBehind".Translate(FoodLabel,
                        pawn.Named("PAWN"),
                        nutritionPerDay.ToStringDecimalIfSmall().Named("NUTRITION"),
                        "PeriodDays".Translate(daysBehind).Named("DURATION"));
                }

                return text;
            }
        }

        public Thing FindFoodFor(Pawn pawnGettingFood)
        {
            ThingOwner<Thing> innerContainer = pawnGettingFood.inventory.innerContainer;
            foreach (Thing item in innerContainer)
            {
                if (FoodValidator(pawnGettingFood, this, item))
                    return item;
            }

            Thing thing = GenClosest.ClosestThingReachable(pawnGettingFood.Position, pawnGettingFood.Map,
                ThingRequest.ForGroup(ThingRequestGroup.FoodSource),
                PathEndMode.ClosestTouch, TraverseParms.For(pawnGettingFood), 9999f, x => FoodValidator(pawnGettingFood, this, x));
            if (thing != null)
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
                    {
                        return item;
                    }
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

        public override void Notify_IngestedThing(Thing food, int numTaken)
        {
            float nutrition = FoodUtility.NutritionForEater(pawn, food);

            if (numTaken > 0)
                nutrition *= numTaken;
            else if (pawn.needs?.food?.NutritionWanted != null)
            {
                // If only part of a corpse was consumed, numTaken will be 0, so assume the pawn eats until full.
                // There doesn't seem to be an easy way to get the nutrition gained directly.
                nutrition = Math.Min(nutrition, pawn.needs.food.NutritionWanted);
            }

            if (ValidateFood(food))
                Severity -= nutrition * SeverityReductionPerNutrition;
        }

        public float NutritionWantedToSatisfy()
        {
            return Severity / SeverityReductionPerNutrition;
        }

        public int ItemsWantedToSatisfy(Thing foodSource, ThingDef foodDef)
        {
            var nutritionNeeded = NutritionWantedToSatisfy();
            var nutritionPerItem = FoodUtility.GetNutrition(pawn, foodSource, foodDef);
            if (nutritionPerItem == 0)
                return 0;
            return Mathf.CeilToInt(nutritionNeeded / nutritionPerItem);
        }

        public bool ValidateFood(Thing food)
        {
            if (food.Destroyed || !food.IngestibleNow)
                return false;

            float nutrition = FoodUtility.NutritionForEater(Gene.pawn, food);
            if (nutrition <= 0.0f)
                return false;

            if (!food.def.IsRawFoodOrCorpse() && RawOnly)
                return false;

            if (FoodKind == FoodUtility.GetFoodKind(food))
                return true;

            var compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients == null)
                return false;
            if (Enumerable.Any(compIngredients.ingredients,
                    ingredient => FoodKind == FoodUtility.GetFoodKind(ingredient)))
                return true;

            return false;
        }

        public void Notify_PostSatisfyGenes()
        {
            Severity = 0;
        }

        public void RegisterWith(NotificationManager manager)
        {
            manager.Register(NotificationEvent.PostSatisfyGenes, pawn, Notify_PostSatisfyGenes);
        }
    }
}
