using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    public class DietDependencyInfo
    {
        public FoodKind foodKind = FoodKind.Any;
        public bool rawOnly = false;
        public float severityReductionPerNutrition = 1f;
        [MustTranslate] public string foodLabel;
    }
}
