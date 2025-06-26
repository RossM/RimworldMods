using System.Linq;
using RimWorld;
using Verse;

public static class Util
{
    public static float GetStatValue(this Pawn eater, string defName, float defaultValue = 1.0f)
    {
        StatDef rawFungusNutritionFactor =
            DefDatabase<StatDef>.GetNamed(defName, errorOnFail: false);
        return rawFungusNutritionFactor != null ? eater.GetStatValue(rawFungusNutritionFactor) : defaultValue;
    }

    public static T FirstGeneOfType<T>(this Pawn pawn) where T : class
    {
        return pawn.genes?.GenesListForReading.OfType<T>().FirstOrDefault();
    }
}