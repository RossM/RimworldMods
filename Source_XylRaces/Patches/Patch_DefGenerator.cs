using System.Linq.Expressions;

namespace XylXenos.Patches;

[HarmonyPatch(typeof(DefGenerator))]
public static class Patch_DefGenerator
{
    [Feature(typeof(GeneDefGenerator))]
    [HarmonyPostfix]
    [HarmonyPatch(nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
    public static void GenerateImpliedDefs_PreResolve_Postfix(bool hotReload)
    {
        foreach (var type in GenTypes.AllTypesWithAttribute<DefGeneratorAttribute>())
        {
            var impliedDefsMethodInfo = type.GetMethod("ImpliedDefs");
            if (impliedDefsMethodInfo == null)
            {
                Log.Error($"{type.Name} is marked as DefGenerator but doesn't have ImpliedDefs method");
                continue;
            }

            var impliedDefsDelegate
                = (Func<bool, IEnumerable<Def>>)impliedDefsMethodInfo.CreateDelegate(typeof(Func<bool, IEnumerable<Def>>));

            Type defType = type.TryGetAttribute<DefGeneratorAttribute>().defType;
            var addImpliedDefMethodInfo = typeof(DefGenerator).GetMethod("AddImpliedDef")!.MakeGenericMethod(defType);
            ParameterExpression defParameter = Expression.Parameter(typeof(Def), "def");
            var addImpliedDefDelegate = Expression.Lambda<Action<Def>>(
                    Expression.Call(addImpliedDefMethodInfo,
                        Expression.Convert(defParameter, defType),
                        Expression.Constant(hotReload)),
                    defParameter)
                .Compile();

            foreach (Def def in impliedDefsDelegate(hotReload))
            {
                addImpliedDefDelegate(def);
            }
        }
    }
}
