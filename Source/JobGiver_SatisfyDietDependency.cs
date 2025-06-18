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
            if (pawn.health.hediffSet.hediffs.Any((Hediff x) => ShouldSatisfy(x)))
            {
                return ThinkNodePriority.Food + 0.01f;
            }
            return 0f;
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            tmpDietDependencies.Clear();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (ShouldSatisfy(hediffs[i]))
                {
                    tmpDietDependencies.Add((Hediff_DietDependency)hediffs[i]);
                }
            }
            if (!tmpDietDependencies.Any())
            {
                return null;
            }
            tmpDietDependencies.SortBy((Hediff_DietDependency x) => 0f - x.Severity);
            for (int num = 0; num < tmpDietDependencies.Count; num++)
            {
                Thing food = FindFoodFor(pawn, tmpDietDependencies[num]);
                if (food != null)
                {
                    var nutritionPer = FoodUtility.NutritionForEater(pawn, food);
                    var nutritionNeeded = tmpDietDependencies[num].Severity / GetGeneFor(pawn, tmpDietDependencies[num])
                        .DefModExtension.severityReductionPerNutrition;
                    var count = Mathf.CeilToInt(nutritionPer / nutritionNeeded);

                    tmpDietDependencies.Clear();
                    Pawn pawn2 = (food.ParentHolder as Pawn_InventoryTracker)?.pawn;
                    Job job;
                    if (pawn2 != null && pawn2 != pawn)
                        job = JobMaker.MakeJob(JobDefOf.TakeFromOtherInventory, food, pawn2);
                    else
                        job = JobMaker.MakeJob(JobDefOf.Ingest, food);
                    job.count = Mathf.Min(food.stackCount, count);
                    return job;
                }
            }
            tmpDietDependencies.Clear();
            return null;
        }

        private bool ShouldSatisfy(Hediff hediff)
        {
            if (!(hediff is Hediff_DietDependency hediff_DietDependency))
            {
                return false;
            }
            return hediff_DietDependency.ShouldSatisfy;
        }

        private Thing FindFoodFor(Pawn pawn, Hediff_DietDependency dependency)
        {
            ThingOwner<Thing> innerContainer = pawn.inventory.innerContainer;
            for (int i = 0; i < innerContainer.Count; i++)
            {
                if (FoodValidator(pawn, dependency, innerContainer[i]))
                {
                    return innerContainer[i];
                }
            }
            Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSource), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, (Thing x) => FoodValidator(pawn, dependency, x));
            if (thing != null)
            {
                return thing;
            }
            if (pawn.IsColonist && pawn.Map != null)
            {
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
            }
            return null;
        }

        private bool FoodValidator(Pawn pawn, Hediff_DietDependency dependency, Thing food)
        {
            if (!food.def.IsIngestible)
                return false;

            Gene_DietDependency gene = GetGeneFor(pawn, dependency);
            if (gene == null)
            {
                Log.Warning(string.Format("DrugValidator: Couldn't find corresponding gene for {0}", dependency));
                return false;
            }

            float nutrition = FoodUtility.NutritionForEater(pawn, food);
            if (nutrition <= 0.0f)
                return false;

            var extension = gene.DefModExtension;
            if (extension.foodKind == FoodUtility.GetFoodKind(food))
                return true;

            if (!food.def.IsProcessedFood)
                return false;
            if (extension.rawOnly)
                return false;

            var compIngredients = food.TryGetComp<CompIngredients>();
            if (compIngredients == null)
                return false;
            if (Enumerable.Any(compIngredients.ingredients, ingredient => extension.foodKind == FoodUtility.GetFoodKind(ingredient)))
                return true;

            return false;
        }

        private static Gene_DietDependency GetGeneFor(Pawn pawn, Hediff_DietDependency dependency)
        {
            var gene = pawn.genes.GenesListForReading.OfType<Gene_DietDependency>().FirstOrDefault(g => g.DefModExtension?.hediffDef == dependency.def);
            return gene;
        }
    }
}
