using System.Reflection;

namespace Xylib;

/// <summary>
///     Contains methods to check coding style and potential bugs or maintainability issues in Harmony patches.
/// </summary>
public static class Analyzer
{
    private static readonly string name = typeof(Analyzer).FullName;

    /// <summary>
    ///     This checks the given assembly for Harmony patches annotated with <see cref="HarmonyPatch" /> and checks
    ///     for issues that might indicate a potential bug or maintainability problem.
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

                // A patch class without [HarmonyPatch] won't get processed, so this almost certainly indicates a bug
                if ((hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && !typeHasHarmony)
                    Log.Warning($"[{name}] {type.FullName}::{method.Name} appears to be a patch but is in a type with no [HarmonyPatch] attribute");

                if (!typeHasHarmony)
                    continue;

                // Putting a [Feature] attribute on each patch helps track which patches do what
                if ((hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && !hasFeature)
                    Log.Warning($"[{name}] {type.FullName}::{method.Name} is missing a [Feature] attribute");

                // [Feature] is only intended for harmony patches
                if (!(hasPrefix || hasPostfix || hasTranspiler || hasInfixPatch) && hasFeature)
                    Log.Warning($"[{name}] {type.FullName}::{method.Name} has [Feature] but no Harmony attribute");

                // Applying [InfixPatch] without [InfixPrefix] or [InfixPostfix], or vice versa, won't do anything, so is probably a bug
                if (hasInfixPatch != (hasInfixPrefix || hasInfixPostfix))
                {
                    Log.Warning(
                        $"[{name}] {type.FullName}::{method.Name} has should have both [InfixPatch] and one of [InfixPrefix] or [InfixPostfix]");
                }

                // Enforce a naming convention for patch methods. This makes it more obvious at a glance when a patch will run
                if ((hasPrefix || hasInfixPrefix) && !(method.Name == "Prefix" || method.Name.EndsWith("_Prefix")))
                    Log.Warning($"[{name}] {type.FullName}::{method.Name} should be named with _Prefix");
                if ((hasPostfix || hasInfixPostfix) && !(method.Name == "Postfix" || method.Name.EndsWith("_Postfix")))
                    Log.Warning($"[{name}] {type.FullName}::{method.Name} should be named with _Postfix");
                if (hasTranspiler && !(method.Name == "Transpiler" || method.Name.EndsWith("_Transpiler")))
                    Log.Warning($"[{name}] {type.FullName}::{method.Name} should be named with _Transpiler");

                var parameters = method.GetParameters();
                ParameterInfo resultParameter = parameters.SingleOrDefault(p => p.Name == "__result");
                if (hasPrefix || hasInfixPrefix)
                {
                    // A prefix __result parameter without 'out' might not be initialized, which results in the default
                    // value being used if the prefix returns false. This is confusing and potentially indicates a bug.
                    if (resultParameter?.IsOut == false)
                        Log.Warning($"[{name}] {type.FullName}::{method.Name} should use 'out' for __result");

                    // If a prefix patch returns void, it will always go on to the main method, and the value of
                    // __result won't be used. This almost certainly indicates a bug.
                    if (method.ReturnType.IsVoid() && resultParameter != null)
                        Log.Warning($"[{name}] {type.FullName}::{method.Name} returns void but uses __result");
                }

                if (hasPostfix || hasInfixPostfix)
                {
                    // Postfix patches taking __result usually want to modify it, which won't work without 'ref',
                    // so a missing 'ref' modifier potentially indicates a bug.
                    if (resultParameter is { ParameterType.IsByRef: false })
                        Log.Warning($"[{name}] {type.FullName}::{method.Name} has a non-ref __result");
                }
            }
        }
    }
}
