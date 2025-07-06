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
        public IntRange? startingItemRange;
    }

    public class Gene_DietDependency : Gene, IGene_HediffSource, IStartingItemGenerator
    {
        public int lastIngestedTick;

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

        public Hediff LinkedHediff
        {
            get
            {
                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                var extension = DefExt;
                if (extension == null)
                    return null;
                return hediffs.FirstOrDefault(hediff => hediff.def == extension?.hediffDef);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastIngestedTick, "lastIngestedTick");
        }

        public override void PostAdd()
        {
            base.PostAdd();
            AddHediff();
            lastIngestedTick = Find.TickManager.TicksGame;
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

            lastIngestedTick = Find.TickManager.TicksGame;
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

            if (food.def.IsProcessedFood && extension.rawOnly)
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

            var extension = DefExt;
            if (extension == null)
            {
                Log.Warning("Gene_DietDependency.ValidateFood called without a GeneDefExtension_DietDependency");
                return false;
            }

            if (food.IsProcessedFood && extension.rawOnly)
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
            if (DefExt?.startingItemRange == null)
                return null;

            var foodDef = DefDatabase<ThingDef>.AllDefsListForReading.Where(thingDef => !thingDef.IsCorpse && ValidateFood(thingDef)).RandomElement();
            if (foodDef == null)
                return null;

            return new(foodDef, Mathf.Clamp(DefExt.startingItemRange.Value.RandomInRange, 1, foodDef.stackLimit));
        }
    }
}