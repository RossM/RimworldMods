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
    public class Gene_DietDependency : Gene
    {
        public int lastIngestedTick;

        public ModExtension_GeneDef_DietDependency DefModExtension => def.GetModExtension<ModExtension_GeneDef_DietDependency>();

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
                var extension = DefModExtension;
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
                var extension = DefModExtension;
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
            var extension = DefModExtension;
            if (extension == null)
            {
                Log.Warning("Gene_DietDependency.Notify_IngestedThing called without a ModExtension_GeneDef_DietDependency");
                return;
            }

            Log.Message(string.Format("Ingested {0} ({1}) x {2}", food.Label, food.def.defName, numTaken));

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

            if (extension.foodKind == FoodUtility.GetFoodKind(food))
            {
                ReduceSeverity(severityReduction);
                return;
            }

            if (!food.def.IsProcessedFood) 
                return;
            if (extension.rawOnly)
                return;

            var compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients == null)
                return;
            if (Enumerable.Any(compIngredients.ingredients, ingredient => extension.foodKind == FoodUtility.GetFoodKind(ingredient)))
            {
                ReduceSeverity(severityReduction);
                return;
            }
        }

        public void ReduceSeverity(float severityReduction)
        {
            var linkedHediff = LinkedHediff;
            if (linkedHediff != null)
            {
                linkedHediff.Severity = Math.Max(linkedHediff.def.initialSeverity, linkedHediff.Severity - severityReduction);
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
    }
}
