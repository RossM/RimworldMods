using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace XylRacesCore.Genes
{
    public class GeneDefExtension_DietDependency : DefModExtension
    {
        public HediffDef hediffDef;
        public FoodKind foodKind = FoodKind.Any;
        public bool rawOnly = false;
        public float severityReductionPerNutrition = 1f;
        public FoodTypeFlags startingFoodType;
        public FloatRange? startingFoodNutrition;
    }

    public class DietDependency : Gene, IGene_HediffSource, IStartingItemSource
    {
        public GeneDefExtension_DietDependency DefExt => def.GetModExtension<GeneDefExtension_DietDependency>();

        public override bool Active
        {
            get
            {
                if (base.Active && pawn != null)
                {
                    return !pawn.IsGhoul;
                }

                return false;
            }
        }

        public Verse.Hediff LinkedHediff
        {
            get
            {
                List<Verse.Hediff> hediffs = pawn.health.hediffSet.hediffs;
                return DefExt == null ? null : hediffs.FirstOrDefault(hediff => hediff.def == DefExt.hediffDef);
            }
        }

        public override void PostAdd()
        {
            base.PostAdd();
            AddHediff();
        }

        private void AddHediff()
        {
            if (Active)
            {
                var extension = DefExt;
                if (extension == null)
                    return;
                var hediff = HediffMaker.MakeHediff(extension.hediffDef, pawn);
                pawn.health.AddHediff(hediff);
            }
        }

        public override void PostRemove()
        {
            var linkedHediff = LinkedHediff;
            if (linkedHediff != null)
            {
                pawn.health.RemoveHediff(linkedHediff);
            }

            base.PostRemove();
        }

        public override void Notify_IngestedThing(Thing food, int numTaken)
        {
            var extension = DefExt;
            if (extension == null)
            {
                Log.Warning(
                    "Gene_DietDependency.Notify_IngestedThing called without a GeneDefExtension_DietDependency");
                return;
            }

            float nutrition = FoodUtility.NutritionForEater(pawn, food);

            if (numTaken > 0)
                nutrition *= numTaken;
            else if (pawn.needs?.food?.NutritionWanted != null)
            {
                // If only part of a corpse was consumed, numTaken will be 0, so assume the pawn eats until full.
                // There doesn't seem to be an easy way to get the nutrition gained directly.
                nutrition = Math.Min(nutrition, pawn.needs.food.NutritionWanted);
            }

            var severityReduction = nutrition * extension.severityReductionPerNutrition;

            if (ValidateFood(food))
                ReduceSeverity(severityReduction);
        }

        public void ReduceSeverity(float severityReduction)
        {
            var linkedHediff = LinkedHediff;
            if (linkedHediff != null)
            {
                linkedHediff.Severity = Math.Max(linkedHediff.def.initialSeverity,
                    linkedHediff.Severity - severityReduction);
            }
            else
            {
                AddHediff();
            }
        }

        public bool ValidateFood(Thing food)
        {
            if (food.Destroyed || !food.IngestibleNow)
                return false;

            float nutrition = FoodUtility.NutritionForEater(pawn, food);
            if (nutrition <= 0.0f)
                return false;

            var extension = DefExt;
            if (extension == null)
            {
                Log.Warning("Gene_DietDependency.ValidateFood called without a GeneDefExtension_DietDependency");
                return false;
            }

            if (!food.def.IsRawFoodOrCorpse() && extension.rawOnly)
                return false;

            if (extension.foodKind == FoodUtility.GetFoodKind(food))
                return true;

            var compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients == null)
                return false;
            if (Enumerable.Any(compIngredients.ingredients,
                    ingredient => extension.foodKind == FoodUtility.GetFoodKind(ingredient)))
                return true;

            return false;
        }

        public bool ValidateFood(ThingDef food)
        {
            if (!food.IsIngestible)
                return false;
            if (food.GetStatBase(StatDefOf.Nutrition) <= 0)
                return false;

            var extension = DefExt;
            if (extension == null)
            {
                Log.Warning("Gene_DietDependency.ValidateFood called without a GeneDefExtension_DietDependency");
                return false;
            }

            if (!food.IsRawFoodOrCorpse() && extension.rawOnly)
                return false;

            if (extension.foodKind == FoodUtility.GetFoodKind(food))
                return true;

            return false;
        }

        public bool CausesHediff(HediffDef hediffDef)
        {
            return DefExt?.hediffDef == hediffDef;
        }

        public ThingDefCount? GetStartingItem()
        {
            if (DefExt?.startingFoodNutrition == null)
                return null;

            var foodDef = DefDatabase<ThingDef>.AllDefsListForReading.Where(GoodStartingFood).RandomElement();
            if (foodDef == null)
                return null;

            float nutritionNeeded = DefExt.startingFoodNutrition.Value.RandomInRange;
            int itemsNeeded = Mathf.CeilToInt(nutritionNeeded / foodDef.GetStatBase(StatDefOf.Nutrition));

            return new(foodDef, Mathf.Clamp(itemsNeeded, 1, foodDef.stackLimit));

            bool GoodStartingFood(ThingDef thingDef)
            {
                if (thingDef.ingestible?.foodType.HasFlag(DefExt.startingFoodType) != true)
                    return false;
                if (!ValidateFood(thingDef))
                    return false;
                return true;
            }
        }

        public float NutritionWantedToSatisfy()
        {
            float severityReductionPerNutrition = DefExt.severityReductionPerNutrition;
            float nutritionForNeed = LinkedHediff.Severity / severityReductionPerNutrition;
            return nutritionForNeed;
        }

        public int ItemsWantedToSatisfy(Thing foodSource, ThingDef foodDef)
        {
            var nutritionNeeded = NutritionWantedToSatisfy();
            var nutritionPerItem = FoodUtility.GetNutrition(pawn, foodSource, foodDef);
            if (nutritionPerItem == 0)
                return 0;
            return Mathf.CeilToInt(nutritionNeeded / nutritionPerItem);
        }
    }
}