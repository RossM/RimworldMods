namespace Xylib;

public static class PatchHelpers
{
    public static float GetJoyFactor(Pawn pawn, JoyGiver joyGiver)
    {
        List<JoyGiverFactor> joyGiverChanceFactors = pawn.GeneTracker?.joyGiverChanceFactors;
        if (joyGiverChanceFactors == null)
            return 1f;

        float factor = 1f;
        foreach (var joyGiverFactor in joyGiverChanceFactors)
        {
            if (joyGiverFactor.joyGiver == joyGiver.def)
                factor *= joyGiverFactor.factor;
        }

        return factor;
    }

    public static void AddDesignators(
        DesignationCategoryDef __instance,
        ref IEnumerable<Designator> __result,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ideoBuildingDesignatorsCached)
    {
        HashSet<Designator> geneDesignators = [];

        foreach (var designators in Enumerable.Select<Pawn, List<BuildableDef>>(Faction.OfPlayer.AllPawns, pawn => pawn.GeneTracker?.addDesignators))
        {
            if (designators == null)
                continue;

            geneDesignators.AddRange(designators.Where(def => def.designationCategory == __instance)
                .Select(GetCachedDesignator));
        }

        if (geneDesignators.Any())
            __result = __result.Concat(geneDesignators);

        Designator GetCachedDesignator(BuildableDef def)
        {
            DesignationCategoryDef.BuildablePreceptBuilding key = new DesignationCategoryDef.BuildablePreceptBuilding(def, null);
            if (!ideoBuildingDesignatorsCached.TryGetValue(key, out var value))
            {
                value = new Designator_Build(def);
                ideoBuildingDesignatorsCached[key] = value;
            }

            return value;
        }
    }
}
