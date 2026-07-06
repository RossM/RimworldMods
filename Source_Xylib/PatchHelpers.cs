namespace Xylib;

internal static class PatchHelpers
{
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
        outGene = pawn.genes?.GenesListForReading.FirstOrDefault(gene => gene.Active && gene.def.Extension_GeneWithComps?.showInDrugPolicies is true);
        return outGene != null;
    }

    public static bool GeneShouldBeVisible(GeneDef geneDef, GeneType geneType)
    {
        var defExt = geneDef.Extension_GeneWithComps;
        if (defExt == null)
            return true;

        if (!defExt.showInXenotypeCreation)
            return false;
        if (defExt.geneType != null && defExt.geneType != geneType)
            return false;

        return true;
    }

    public static IEnumerable<string> GetGeneEffectDescriptions(this GeneDef geneDef)
    {
        if (geneDef.Extension_GeneWithComps is { } defExt)
        {
            foreach (var customEffectDescription in defExt.CustomEffectDescriptions)
                yield return customEffectDescription;
        }
    }

    public static float GetRangedDodgeChance(Pawn target)
    {
        if (target.DeadOrDowned)
            return 0;
        if (target.GetPosture() != PawnPosture.Standing)
            return 0;

        return target.GetStatValue(XStatDefOf.XylRangedDodgeChance);
    }

    public static void AddSlaveRebellionMtbFactorExplanation(StringBuilder stringBuilder, Pawn pawn)
    {
        if (pawn == null)
            return;

        StatRequest statRequest = StatRequest.For(pawn);
        float baseValueFor = XStatDefOf.XylSlaveRebellionMtbFactor.Worker.GetBaseValueFor(statRequest);
        ToStringNumberSense toStringNumberSense = XStatDefOf.XylSlaveRebellionMtbFactor.toStringNumberSense;
        XStatDefOf.XylSlaveRebellionMtbFactor.Worker.GetOffsetsAndFactorsExplanation(statRequest, stringBuilder, baseValueFor);
        XStatDefOf.XylSlaveRebellionMtbFactor.Worker.GetAdditionalOffsetsAndFactorsExplanation(statRequest, toStringNumberSense,
            stringBuilder);
    }

    public static bool IsRecipeUnlockedByGenes(RecipeDef recipe)
    {
        foreach (var pawn in Faction.OfPlayer.AllAlivePawns)
        {
            if (pawn.GeneTracker_Xylib?.unlockedRecipes?.Contains(recipe) is true)
                return true;
        }

        return false;
    }

    public static HashSet<RecipeDef> GetRecipesUnlockedByGenes()
    {
        HashSet<RecipeDef> result = [];
        foreach (var geneDef in DefDatabase<GeneDef>.AllDefs)
        {
            var recipes = geneDef.CompProps<GeneCompProperties_UnlockRecipes>()?.recipes;
            if (recipes != null)
                result.AddRange(recipes);
        }

        return result;
    }

    public static void AddDesignators(
        DesignationCategoryDef __instance,
        ref IEnumerable<Designator> __result,
        Dictionary<DesignationCategoryDef.BuildablePreceptBuilding, Designator> ideoBuildingDesignatorsCached)
    {
        HashSet<Designator> geneDesignators = [];

        foreach (var designators in Faction.OfPlayer.AllAlivePawns.Select(pawn => pawn.GeneTracker_Xylib?.unlockedBuildables))
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

    public static HashSet<RecipeDef> RecipesUnlockedByGenes => field ??= Xylib.PatchHelpers.GetRecipesUnlockedByGenes();

    public static float GetHediffResistance(Pawn pawn, HediffDef def)
    {
        if (def == HediffDefOf.BloodLoss)
            return pawn.GetStatValue(XStatDefOf.XylBloodLossResistance);
        if (def == HediffDefOf.DrugOverdose)
            return pawn.GetStatValue(XStatDefOf.XylDrugOverdoseResistance);
        if (def == HediffDefOf.Heatstroke)
            return pawn.GetStatValue(XStatDefOf.XylHeatstrokeResistance);
        if (def == HediffDefOf.Hypothermia)
            return pawn.GetStatValue(XStatDefOf.XylHypothermiaResistance);
        if (def == HediffDefOf.Malnutrition)
            return pawn.GetStatValue(XStatDefOf.XylMalnutritionResistance);

        return 0f;
    }
}
