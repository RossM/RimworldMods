using System.Reflection;

namespace Xylib;

/// <summary>
///     Contains methods to check coding style and potential bugs or maintainability issues in Harmony patches.
/// </summary>
[PublicAPI]
public static class Analyzer
{
    private const BindingFlags MethodBindingFlags
        = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly string name = typeof(Analyzer).FullName!;

    private static readonly Type[] defTypes =
    [
        typeof(AbilityCompProperties),
        typeof(ColorGenerator),
        typeof(CompProperties),
        typeof(Def),
        typeof(DefModExtension),
        typeof(HediffCompProperties),
        typeof(HediffGiver),
        typeof(IngestionOutcomeDoer),
        typeof(PatchOperation),
        typeof(PawnRenderNodeProperties),
        typeof(ScenPart),
    ];

    /// <summary>
    ///     This checks the given assembly for Harmony patches annotated with <see cref="HarmonyPatch" /> and checks
    ///     for issues that might indicate a potential bug or maintainability problem.
    /// </summary>
    /// <param name="assembly"></param>
    public static void CheckCodingStyle_Patches(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        foreach (Type type in assembly.GetTypes())
        {
            bool typeHasHarmony = type.HasAttribute<HarmonyPatch>();

            if (typeHasHarmony && type is not { IsAbstract: true, IsSealed: true })
                Log.Warning($"[{name}] {type.FullName} should be static");

            foreach (MethodInfo method in type.GetMethods())
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
                    Log.Warning(
                        $"[{name}] {type.FullName}::{method.Name} appears to be a patch but is in a type with no [HarmonyPatch] attribute");

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
                ParameterInfo? resultParameter = parameters.SingleOrDefault(p => p.Name == "__result");
                if (hasPrefix || hasInfixPrefix)
                {
                    // A prefix __result parameter without 'out' might not be initialized, which results in the default
                    // value being used if the prefix returns false. This is confusing and potentially indicates a bug.
                    if (resultParameter?.IsOut is false)
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

    public static void CheckCodingStyle_Defs(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        List<Type> extraDefTypes = GenTypes.AllTypesWithAttribute<UsedFromXmlAttribute>().Where(t => t.IsAbstract).ToList();

        foreach (Type type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.HasAttribute<UsedFromXmlAttribute>())
                continue;

            foreach (var defType in defTypes)
            {
                if (defType.IsAssignableFrom(type))
                    Log.Warning($"[{name}] {type.FullName} is a {defType.Name} but is missing a [UsedFromXml] attribute");
            }

            foreach (var defType in extraDefTypes)
            {
                if (defType.IsAssignableFrom(type))
                    Log.Warning($"[{name}] {type.FullName} is a {defType.Name} but is missing a [UsedFromXml] attribute");
            }
        }
    }

    public static void CheckCodingStyle_ConfigErrors(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        foreach (Type type in assembly.GetTypes())
        {
            MethodInfo? method = type.GetMethod("ConfigErrors", MethodBindingFlags);
            if (method == null)
                continue;

            MethodInfo? baseMethod = ReflectionHelpers.GetBaseMethod(method);
            if (baseMethod == null)
                continue;

            List<CodeInstruction>? instructions = ReflectionHelpers.GetInstructions(method);

            if (!instructions.Any(inst => inst.operand is MethodInfo m && ReflectionHelpers.PossiblyWrappedTargetIs(m, baseMethod)))
            {
                Log.Warning(
                    $"[{name}] {type.FullName}::{method.Name} is missing a call to {baseMethod.DeclaringType?.FullName}::{baseMethod.Name}");
            }
        }
    }

    public static void CheckCodingStyle_ExposeData(Assembly assembly)
    {
        if (assembly is null)
            throw new ArgumentNullException(nameof(assembly));

        foreach (Type type in assembly.GetTypes())
        {
            string? methodName =
                typeof(IExposable).IsAssignableFrom(type) ? "ExposeData" :
                typeof(ThingComp).IsAssignableFrom(type) ? "PostExposeData" :
                IsComp(type) ? "CompExposeData" :
                null;
            if (methodName == null)
                continue;

            MethodInfo? method = type.GetMethod(methodName, MethodBindingFlags);

            var fieldsToSave = type.GetFields().Where(field => ShouldExposeField(field, type)).ToList();

            if (fieldsToSave.Count == 0)
                continue;

            List<FieldInfo> savedFields = [];
            if (method is not null)
            {
                var instructions = PatchProcessor.GetOriginalInstructions(method);
                DebugAssert.NotNull(instructions);

                MethodInfo? baseMethod = ReflectionHelpers.GetBaseMethod(method);
                bool callsBaseMethod = false;

                foreach (var inst in instructions)
                {
                    if (inst.operand is FieldInfo f && f.DeclaringType == type)
                        savedFields.Add(f);
                    if (baseMethod is not null && inst.operand is MethodInfo m && ReflectionHelpers.PossiblyWrappedTargetIs(m, baseMethod))
                        callsBaseMethod = true;
                }

                if (baseMethod is not null && !callsBaseMethod)
                {
                    Type baseType = baseMethod.DeclaringType;
                    DebugAssert.NotNull(baseType);

                    if (baseType.GetFields().Any(field => ShouldExposeField(field, baseType)))
                    {
                        Log.Warning(
                            $"[{name}] {type.FullName}::{method.Name} is missing a call to {baseType.FullName}::{baseMethod.Name}");
                    }
                }
            }

            foreach (var field in fieldsToSave.Except(savedFields))
            {
                Log.Warning(
                    $"[{name}] {type.FullName}::{field.Name} appears to not be saved in {methodName}"); 
                Log.WarningOnce($"[{name}] Either save this field, mark it [Unsaved], or make it const or readonly", 0x49D9F6A4);
            }
        }

        static bool IsComp(Type type)
        {
            for (Type? t = type; t != null; t = t.BaseType)
            {
                if (t.Name.EndsWith("Comp"))
                    return true;
            }

            return false;
        }

        static bool ShouldExposeField(FieldInfo field, Type type) =>
            field.GetCustomAttribute<UnsavedAttribute>() == null &&
            !field.Attributes.HasFlag(FieldAttributes.Literal) &&
            !field.Attributes.HasFlag(FieldAttributes.Static) &&
            field.DeclaringType == type;
    }

    public static void CheckCodingStyle(Assembly assembly)
    {
        CheckCodingStyle_Patches(assembly);
        CheckCodingStyle_Defs(assembly);
        CheckCodingStyle_ConfigErrors(assembly);
        CheckCodingStyle_ExposeData(assembly);
    }
}
