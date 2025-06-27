using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;

namespace XylRacesCore
{
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
            tmpDietDependencies.SortBy((Hediff_DietDependency x) => 0f - x.Severity);
            try
            {
                foreach (Hediff_DietDependency dietDependency in tmpDietDependencies)
                {
                    Thing food = FindFoodFor(pawn, dietDependency);
                    if (food == null)
                        continue;

                    float nutritionPer = FoodUtility.NutritionForEater(pawn, food);
                    float severityReductionPerNutrition = dietDependency.Gene.DefExt.severityReductionPerNutrition;
                    float nutritionNeeded = dietDependency.Severity / severityReductionPerNutrition;
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

        private Thing FindFoodFor(Pawn pawn, Hediff_DietDependency dependency)
        {
            ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;
            foreach (Thing item in innerContainer)
            {
                if (FoodValidator(pawn, dependency, item))
                    return item;
            }
            Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSource), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing x) => FoodValidator(pawn, dependency, x));
            if (thing != null)
                return thing;

            if (!pawn.IsColonist || pawn.Map == null) 
                return null;
            
            foreach (Pawn spawnedColonyAnimal in pawn.Map.mapPawns.SpawnedColonyAnimals)
            {
                foreach (Thing item in spawnedColonyAnimal.inventory.innerContainer)
                {
                    if (FoodValidator(pawn, dependency, item) && !spawnedColonyAnimal.IsForbidden(pawn) && pawn.CanReach(spawnedColonyAnimal, PathEndMode.OnCell, Danger.Some))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        private static bool FoodValidator(Pawn pawn, Hediff_DietDependency dependency, Thing food)
        {
            if (!food.def.IsIngestible)
                return false;
            if (food.IsForbidden(pawn))
                return false;

            Gene_DietDependency gene = dependency.Gene;
            if (gene == null)
            {
                Log.Warning(string.Format("FoodValidator: Couldn't find corresponding gene for {0}", dependency));
                return false;
            }

            return gene.ValidateFood(food);
        }
    }
}
