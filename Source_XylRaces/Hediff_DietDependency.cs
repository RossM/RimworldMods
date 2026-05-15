using RimWorld;
using Verse;
using Verse.AI;
using XylRacesCore.Genes;

namespace XylRacesCore
{
    public class Hediff_DietDependency : Hediff_Genetic
    {
        enum Stages : int
        {
            Satisfied = 1,
            Craving = 2,
            MildDeficiency = 3,
            SevereDeficiency = 4,
            Coma = 5,
        }

        public bool ShouldSatisfy => Severity >= def.stages[(int)Stages.Craving].minSeverity;

        public new DietDependency Gene => (DietDependency)base.Gene;

        public float SeverityReductionPerNutrition => Gene.DefExt.severityReductionPerNutrition;

        public Thing FindFoodFor(Pawn pawn2)
        {
            ThingOwner<Thing> innerContainer = pawn2.inventory.innerContainer;
            foreach (Thing item in innerContainer)
            {
                if (FoodValidator(pawn2, this, item))
                    return item;
            }

            Thing thing = GenClosest.ClosestThingReachable(pawn2.Position, pawn2.Map, ThingRequest.ForGroup(ThingRequestGroup.FoodSource),
                PathEndMode.ClosestTouch, TraverseParms.For(pawn2), 9999f, x => FoodValidator(pawn2, this, x));
            if (thing != null)
                return thing;

            if (!pawn2.IsColonist || pawn2.Map == null)
                return null;

            foreach (Pawn spawnedColonyAnimal in pawn2.Map.mapPawns.SpawnedColonyAnimals)
            {
                foreach (Thing item in spawnedColonyAnimal.inventory.innerContainer)
                {
                    if (FoodValidator(pawn2, this, item) && !spawnedColonyAnimal.IsForbidden(pawn2)
                                                         && pawn2.CanReach(spawnedColonyAnimal, PathEndMode.OnCell, Danger.Some))
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

            DietDependency gene = dependency.Gene;
            if (gene == null)
            {
                Log.Warning($"FoodValidator: Couldn't find corresponding gene for {dependency}");
                return false;
            }

            return gene.ValidateFood(food);
        }

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
                    text += "GeneDefChemicalNeedDurationDesc".Translate(Gene.DefExt.foodLabel,
                        pawn.Named("PAWN"),
                        "PeriodDays".Translate(deficiencyDays).Named("DEFICIENCYDURATION"),
                        "PeriodDays".Translate(comaDays).Named("COMADURATION"),
                        "PeriodDays".Translate(deathDays).Named("DEATHDURATION")).Resolve();
                    float daysBehind = Severity / severityPerDay;
                    float nutritionPerDay = severityPerDay * Gene.DefExt.severityReductionPerNutrition;
                    text += "\n\n" + "XyInjestedBehind".Translate(Gene.DefExt.foodLabel,
                        pawn.Named("PAWN"),
                        nutritionPerDay.ToStringDecimalIfSmall().Named("NUTRITION"),
                        "PeriodDays".Translate(daysBehind).Named("DURATION"));
                }

                return text;
            }
        }
    }
}
