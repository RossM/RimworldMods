using RimWorld.Planet;

namespace XylXenos;

[Patch(typeof(CaravanPawnsNeedsUtility))]
public static class Patch_CaravanPawnsNeedsUtility
{
    [Feature(typeof(Hediff_DietDependency))]
    [Postfix]
    [Target(nameof(CaravanPawnsNeedsUtility.GetFoodScore), typeof(Thing), typeof(Pawn))]
    public static void GetFoodScore_Postfix(Thing food, Pawn pawn, ref float __result)
    {
        __result += PatchHelpers.FoodOptimalityBonus(pawn, food);
    }
}
