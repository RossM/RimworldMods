using RimWorld;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    public class Hediff_DietDependency : Hediff_Genetic
    {
        public bool ShouldSatisfy => Severity >= def.stages[2].minSeverity - 0.5f;

        public new Gene_DietDependency Gene => (Gene_DietDependency)base.Gene;

        public float SeverityReductionPerNutrition => Gene.DefExt.severityReductionPerNutrition;

        public Thing FindFoodFor(Pawn pawn2)
        {
            ThingOwner<Thing> innerContainer = pawn2.inventory.innerContainer;
            foreach (Thing item in innerContainer)
            {
                if (FoodValidator(pawn2, this, item))
                    return item;
            }
            Thing thing = GenClosest.ClosestThingReachable(pawn2.Position, pawn2.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSource), PathEndMode.ClosestTouch, TraverseParms.For(pawn2), 9999f, x => FoodValidator(pawn2, this, x));
            if (thing != null)
                return thing;

            if (!pawn2.IsColonist || pawn2.Map == null) 
                return null;
            
            foreach (Pawn spawnedColonyAnimal in pawn2.Map.mapPawns.SpawnedColonyAnimals)
            {
                foreach (Thing item in spawnedColonyAnimal.inventory.innerContainer)
                {
                    if (FoodValidator(pawn2, this, item) && !spawnedColonyAnimal.IsForbidden(pawn2) && pawn2.CanReach(spawnedColonyAnimal, PathEndMode.OnCell, Danger.Some))
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
            if (!pawn.CanReserve(food))
                return false;

            Gene_DietDependency gene = dependency.Gene;
            if (gene == null)
            {
                Log.Warning($"FoodValidator: Couldn't find corresponding gene for {dependency}");
                return false;
            }

            return gene.ValidateFood(food);
        }
    }
}
