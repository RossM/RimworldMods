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

    public static void RunDefGenerators(bool hotReload)
    {
        foreach (var type in GenTypes.AllTypesWithAttribute<DefGeneratorAttribute>())
        {
            using var _ = new ProfileBlock(type.FullName);

            try
            {
                Type defType = type.TryGetAttribute<DefGeneratorAttribute>().defType;

                var impliedDefsMethodInfo = type.GetMethod("ImpliedDefs");
                if (impliedDefsMethodInfo == null)
                {
                    Log.Error($"{type.FullName} is marked as DefGenerator but doesn't have ImpliedDefs method");
                    continue;
                }

                var addDefsFn = typeof(PatchHelpers).GetMethod(nameof(AddDefs))!.MakeGenericMethod(defType)
                    .CreateDelegate<Action<IEnumerable<Def>, bool>>();
                var impliedDefsFn = impliedDefsMethodInfo.CreateDelegate<Func<bool, IEnumerable<Def>>>();

                addDefsFn(impliedDefsFn(hotReload), hotReload);
            }
            catch (Exception e)
            {
                Log.Error($"Error running def generator {type.FullName}: {e}");
            }
        }
    }

    [UsedFromReflection]
    public static void AddDefs<T>(IEnumerable<Def> defs, bool hotReload) where T : Def, new()
    {
        foreach (var def in defs)
        {
            DefGenerator.AddImpliedDef((T)def, hotReload);
        }
    }

    public static bool TryGetChemicalDependencyGene(Pawn pawn, out Gene outGene)
    {
        outGene = pawn.genes?.GenesListForReading.FirstOrDefault(gene => gene.Active && gene.def.DefExt?.showInDrugPolicies == true);
        return outGene != null;
    }

    public static bool GeneShouldBeVisible(GeneDef geneDef, GeneType geneType)
    {
        var defExt = geneDef.DefExt;
        if (defExt == null)
            return true;

        if (!defExt.showInXenotypeCreation)
            return false;
        if (defExt.geneType != null && defExt.geneType != geneType)
            return false;

        return true;
    }
}
