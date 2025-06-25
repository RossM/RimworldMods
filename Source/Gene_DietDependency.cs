using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace XylRacesCore
{
    public class ModExtension_GeneDef_DietDependency : DefModExtension
    {
        public HediffDef hediffDef;
        public FoodKind foodKind = FoodKind.Any;
        public bool rawOnly = false;
        public float severityReductionPerNutrition = 1f;
    }

    public class Gene_DietDependency : Gene
    {
        public int lastIngestedTick;

        public ModExtension_GeneDef_DietDependency DefExt => def.GetModExtension<ModExtension_GeneDef_DietDependency>();

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
                    "Gene_DietDependency.Notify_IngestedThing called without a ModExtension_GeneDef_DietDependency");
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastIngestedTick, "lastIngestedTick", 0);
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
                Log.Warning("Gene_DietDependency.ValidateFood called without a ModExtension_GeneDef_DietDependency");
                return false;
            }

            if (extension.foodKind == FoodUtility.GetFoodKind(food))
                return true;

            if (!food.def.IsProcessedFood)
                return false;
            if (extension.rawOnly)
                return false;

            var compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients == null)
                return false;
            if (Enumerable.Any(compIngredients.ingredients,
                    ingredient => extension.foodKind == FoodUtility.GetFoodKind(ingredient)))
                return true;

            return false;
        }
    }
}