using System.Reflection;

namespace Xylib;

public static class Analyzer
{
    /// <summary>
    /// This checks the given assembly for Harmony patches annotated with <see cref="HarmonyPatch"/> and checks
    /// for issues that might indicate a potential bug or maintainability problem.
    /// </summary>
    /// <param name="assembly"></param>
    public static void CheckCodingStyle_Patches(Assembly assembly)
    {
        foreach (TypeInfo type in assembly.DefinedTypes)
        {
            bool typeHasHarmony = type.HasAttribute<HarmonyPatch>();

            foreach (MethodInfo method in type.DeclaredMethods)
            {
                var hasFeature = method.HasAttribute<FeatureAttribute>();
                var hasPrefix = method.HasAttribute<HarmonyPrefix>();
                var hasPostfix = method.HasAttribute<HarmonyPostfix>();
                var hasTranspiler = method.HasAttribute<HarmonyTranspiler>();
                var hasInfixPatch = method.HasAttribute<InfixPatchAttribute>();
                var hasInfixPrefix = method.HasAttribute<InfixPrefixAttribute>();
                var hasInfixPostfix = method.HasAttribute<InfixPostfixAttribute>();

                if ((hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && !typeHasHarmony)
                    Log.Warning($"{type.FullName}::{method.Name} appears to be a patch but is in a type with no [HarmonyPatch] attribute");

                if (!typeHasHarmony)
                    continue;

                if ((hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && !hasFeature)
                    Log.Warning($"{type.FullName}::{method.Name} is missing a [Feature] attribute");
                if (!(hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && hasFeature)
                    Log.Warning($"{type.FullName}::{method.Name} has [Feature] but no Harmony attribute");

                if (hasInfixPatch != (hasInfixPrefix || hasInfixPostfix))
                    Log.Warning(
                        $"{type.FullName}::{method.Name} has should have both [InfixPatch] and one of [InfixPrefix] or [InfixPostfix]");

                if ((hasPrefix || hasInfixPrefix) && !(method.Name == "Prefix" || method.Name.EndsWith("_Prefix")))
                    Log.Warning($"{type.FullName}::{method.Name} should be named with _Prefix");
                if ((hasPostfix || hasInfixPostfix) && !(method.Name == "Postfix" || method.Name.EndsWith("_Postfix")))
                    Log.Warning($"{type.FullName}::{method.Name} should be named with _Postfix");
                if (hasTranspiler && !(method.Name == "Transpiler" || method.Name.EndsWith("_Transpiler")))
                    Log.Warning($"{type.FullName}::{method.Name} should be named with _Transpiler");

                var parameters = method.GetParameters();
                ParameterInfo resultParameter = parameters.SingleOrDefault(p => p.Name == "__result");
                if (hasPrefix || hasInfixPrefix)
                {
                    if (resultParameter?.IsOut == false)
                        Log.Warning($"{type.FullName}::{method.Name} should use 'out' for __result");
                    if (method.ReturnType.IsVoid() && resultParameter != null)
                        Log.Warning($"{type.FullName}::{method.Name} returns void but uses __result");
                }

                if (hasPostfix || hasInfixPostfix)
                {
                    if (resultParameter is { ParameterType.IsByRef: false })
                        Log.Warning($"{type.FullName}::{method.Name} has a non-ref __result");
                }
            }
        }
    }
}