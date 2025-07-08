using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class JobGiver_SatisfyDietDependency : ThinkNode_JobGiver
    {
        private static readonly List<Hediff_DietDependency> tmpDietDependencies = new List<Hediff_DietDependency>();

        public override float GetPriority(Pawn pawn)
        {
            if (pawn.health.hediffSet.hediffs.Any(hediff => hediff is Hediff_DietDependency { ShouldSatisfy: true }))
            {
                return ThinkNodePriority.Food + 0.01f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            tmpDietDependencies.Clear();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            foreach (Hediff hediff in hediffs)
            {
                if (hediff is Hediff_DietDependency { ShouldSatisfy: true } dietDependency)
                {
                    tmpDietDependencies.Add(dietDependency);
                }
            }
            if (!tmpDietDependencies.Any())
            {
                return null;
            }
            tmpDietDependencies.SortBy(x => 0f - x.Severity);
            try
            {
                foreach (Hediff_DietDependency dietDependency in tmpDietDependencies)
                {
                    Thing food = dietDependency.FindFoodFor(pawn);
                    if (food == null)
                        continue;

                    float nutritionPer = FoodUtility.NutritionForEater(pawn, food);
                    float nutritionNeeded = dietDependency.Severity / dietDependency.SeverityReductionPerNutrition;
                    int count = Mathf.CeilToInt(nutritionNeeded / nutritionPer);

                    Pawn pawn2 = (food.ParentHolder as Pawn_InventoryTracker)?.pawn;
                    Job job;
                    if (pawn2 != null && pawn2 != pawn)
                        job = JobMaker.MakeJob(JobDefOf.TakeFromOtherInventory, food, pawn2);
                    else
                        job = JobMaker.MakeJob(JobDefOf.Ingest, food);
                    job.count = Mathf.Min(food.stackCount, count);
                    job.ingestTotalCount = true;
                    return job;
                }

                return null;
            }
            finally
            {
                tmpDietDependencies.Clear();
            }
        }
    }
}
