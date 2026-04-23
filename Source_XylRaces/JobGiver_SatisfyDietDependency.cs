using RimWorld;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
    [UsedImplicitly]
    public class JobGiver_SatisfyDietDependency : ThinkNode_JobGiver
    {
        private static readonly List<Hediff_DietDependency> tmpDietDependencies = [];

        public override float GetPriority(Pawn pawn)
        {
            if (pawn.HediffsOfType<Hediff_DietDependency>().Any(h => h.CurStageIndex >= 2))
            {
                return ThinkNodePriority.Food + 0.01f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            tmpDietDependencies.Clear();
            tmpDietDependencies.AddRange(pawn.HediffsOfType<Hediff_DietDependency>().Where(h => h.ShouldSatisfy));
            if (!tmpDietDependencies.Any())
                return null;
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
