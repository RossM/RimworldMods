using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xylib;

internal static class PatchHelpers
{
    public static HashSet<RecipeDef> RecipesUnlockedByGenes => field ??= GetRecipesUnlockedByGenes();

    private static Dictionary<HediffDef, StatDef> ResistanceStatByHediff => field ??= Config.Instance.resistanceStatByHediff;

    private static readonly MethodInfo addDefsMethodInfo = typeof(PatchHelpers).GetMethod(nameof(AddDefs))!;

    private static readonly ConcurrentDictionary<Type, Action<object, List<string>>?> requiredMemberCheckerCache = new();
    private static readonly MethodInfo stringListAddMethod = typeof(List<string>).GetMethod("Add", [typeof(string)]);

    public static void RunDefGenerators(bool hotReload)
    {
        foreach (var type in GenTypes.AllTypesWithAttribute<DefGeneratorAttribute>())
        {
            using var _ = new ProfileBlock(type.FullName!);

            try
            {
                Type? defType = type.TryGetAttribute<DefGeneratorAttribute>()?.defType;
                DebugAssert.NotNull(defType);

                var impliedDefsMethodInfo = type.GetMethod("ImpliedDefs");
                if (impliedDefsMethodInfo == null)
                {
                    Log.Error($"{type.FullName} is marked as DefGenerator but doesn't have ImpliedDefs method");
                    continue;
                }

                var addDefsFn = addDefsMethodInfo.MakeGenericMethod(defType)
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

    public static bool TryGetChemicalDependencyGene(Pawn pawn, [NotNullWhen(true)] out Gene? outGene)
    {
        outGene = (pawn.genes?.GenesListForReading).FirstOrDefault(gene =>
            gene.Active && gene.def.Extension_GeneWithComps?.showInDrugPolicies is true);
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

    public static void AddSlaveRebellionMtbFactorExplanation(StringBuilder stringBuilder, Pawn? pawn)
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

    public static float GetHediffResistance(Pawn pawn, HediffDef def)
    {
        return ResistanceStatByHediff.TryGetValue(def, out var stat) ? pawn.GetStatValue(stat) : 0f;
    }

    public static List<GeneDef> FilterGenes(List<GeneDef> genes, bool inheritable, bool ignoreRestrictions)
    {
        if (ignoreRestrictions)
            return genes;
        return genes.Where(g => GeneShouldBeVisible(g, inheritable ? GeneType.Endogene : GeneType.Xenogene)).ToList();
    }

    public static bool IsRequiredField(MemberInfo member)
    {
        // RimWorld's version of .NET doesn't include RequiredMemberAttribute, so we look for any attribute class with the right name
        Attribute[] customAttributes = Attribute.GetCustomAttributes(member, inherit: true);
        return customAttributes.Any(a => a.GetType().FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");
    }

    private static Action<object, List<string>>? GetRequiredMemberCheckerFunc(Type defType)
    {
        if (requiredMemberCheckerCache.TryGetValue(defType, out var checkerFunc))
            return checkerFunc;

        List<FieldInfo> requiredFields = [];
        for (var type = defType; type is not null; type = type.BaseType)
        {
            requiredFields.AddRange(type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(IsRequiredField));
        }

        if (requiredFields.Count == 0)
        {
            requiredMemberCheckerCache[defType] = checkerFunc = null;
            return checkerFunc;
        }

        Debug.Log($"{defType.FullName} requires: {string.Join(", ", requiredFields.Select(f => f.Name))}");

        var defParamExpr = Expression.Parameter(typeof(object), "def");
        var errorsParamExpr = Expression.Parameter(typeof(List<string>), "errors");
        var defExpr = Expression.Convert(defParamExpr, defType);

        List<Expression> checkExpressions = [];
        foreach (var field in requiredFields)
        {
            // Creates an expression representing:
            //   if (def.field == null) errors.Add("field is required");
            checkExpressions.Add(
                Expression.IfThen(
                    Expression.Equal(
                        Expression.Field(defExpr, field),
                        Expression.Constant(null)),
                    Expression.Call(
                        errorsParamExpr,
                        stringListAddMethod,
                        Expression.Constant($"{field.Name} is required"))));
        }

        var blockExpr = Expression.Block(checkExpressions);
        var lambdaExpr = Expression.Lambda<Action<object, List<string>>>(blockExpr, defParamExpr, errorsParamExpr);

        requiredMemberCheckerCache[defType] = checkerFunc = lambdaExpr.Compile();
        return checkerFunc;
    }

    public static List<string>? RequiredMemberErrors(object def)
    {
        var checkerFunc = GetRequiredMemberCheckerFunc(def.GetType());
        if (checkerFunc == null)
            return null;

        List<string> errors = [];
        checkerFunc(def, errors);

        return errors;
    }
}
