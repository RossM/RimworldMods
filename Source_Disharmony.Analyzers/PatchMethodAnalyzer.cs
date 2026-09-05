using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Disharmony.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed partial class PatchMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [
            GenericMethod, StaticMethod, PrefixReturn, PostfixReturn, AlwaysRunReturn, MissingPatchClass, MissingTarget, MissingPatchType,
            MultiplePatchTypes, MultipleInnerTargets, MissingTargetType, NullInnerConstant,
            DuplicateDiscoveryAttributes, MissingMemberName,
            MultipleParameterBindings, InnerBindingWithoutInnerPatch, AlwaysRunResultBinding, InvalidExceptionBinding,
            InvalidDelegateBinding, IncompatibleBindingType, IncompatibleStateTypes, ConstantBindingUnavailable,
            VoidPrefixResultBinding, ReadOnlyPrefixResultBinding, UnknownSpecialParameter, DuplicateBinding, StateWithoutWriter,
        ];

    private static readonly DiagnosticDescriptor GenericMethod = new(
        "DH0001", "Patch method must not contain generic parameters",
        "Patch method '{0}' has generic parameters on the method or a containing type; use a non-generic method in a non-generic type",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor StaticMethod = new(
        "DH0002", "Patch method must be static", "Patch method '{0}' is not static; add the static modifier",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PrefixReturn = new(
        "DH0003", "Prefix must return bool or void", "Prefix '{0}' must return bool or void",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor PostfixReturn = new(
        "DH0004", "Postfix must return void", "Postfix '{0}' must return void",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AlwaysRunReturn = new(
        "DH0005", "AlwaysRun prefix must return void", "Prefix '{0}' with AlwaysRun must return void",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingPatchClass = new(
        "DH0006", "Patch method requires a discoverable containing class",
        "Patch method '{0}' requires [Patch] or [HarmonyPatch] on its containing class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingTarget = new(
        "DH0007", "Patch method requires a target attribute",
        "Patch method '{0}' requires [Target] or [Targets] on the method or its containing class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingPatchType = new(
        "DH0008", "Disharmony method attributes require a patch type",
        "Method '{0}' has a Disharmony attribute but no [Prefix] or [Postfix]; add the appropriate patch attribute or remove the unused Disharmony attribute",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var prefix = start.Compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
            var postfix = start.Compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
            var patch = start.Compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
            var harmonyPatch = start.Compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatch");
            var target = start.Compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
            var targets = start.Compilation.GetTypeByMetadataName("Disharmony.TargetsAttribute");
            var options = start.Compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
            var flags = start.Compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
            var alwaysRun = flags?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
            var methodAttributes = new[]
            {
                prefix, postfix, target, targets, options,
                start.Compilation.GetTypeByMetadataName("Disharmony.InnerAttribute"),
                start.Compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute"),
                start.Compilation.GetTypeByMetadataName("Disharmony.PriorityAttribute"),
            };
            if (prefix is null && postfix is null)
                return;

            RegisterRegistryChecks(start);
            RegisterParameterChecks(start);

            start.RegisterSymbolAction(ctx =>
            {
                var method = (IMethodSymbol)ctx.Symbol;
                bool isPrefix = FindAttribute(method, prefix) is not null;
                bool isPostfix = FindAttribute(method, postfix) is not null;
                var location = method.Locations.FirstOrDefault(l => l.IsInSource);
                if (location is null)
                    return;
                if (!isPrefix && !isPostfix)
                {
                    // Class defaults, return attributes, and parameter bindings do not mark a helper as a patch.
                    if (method.GetAttributes().Any(a => IsAttribute(a, methodAttributes)))
                        ctx.ReportDiagnostic(Diagnostic.Create(MissingPatchType, location, method.Name));
                    return;
                }

                if (FindAttribute(method.ContainingType, patch) is null &&
                    FindAttribute(method.ContainingType, harmonyPatch) is null)
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingPatchClass, location, method.Name));
                if (FindAttribute(method, target, targets) is null && FindAttribute(method.ContainingType, target, targets) is null)
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingTarget, location, method.Name));
                if (HasGenericParameters(method))
                    ctx.ReportDiagnostic(Diagnostic.Create(GenericMethod, location, method.Name));
                if (!method.IsStatic)
                    ctx.ReportDiagnostic(Diagnostic.Create(StaticMethod, location, method.Name));

                // Reflection's ReturnType distinguishes bool from bool&, unlike Roslyn's ReturnType.
                bool returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                                   !method.ReturnsByRef && !method.ReturnsByRefReadonly;
                if (isPrefix)
                {
                    bool runsAlways = alwaysRun is int mask && (GetPatchOptions(method, options) & mask) != 0;
                    if (runsAlways && !method.ReturnsVoid)
                        ctx.ReportDiagnostic(Diagnostic.Create(AlwaysRunReturn, location, method.Name));
                    else if (!method.ReturnsVoid && !returnsBool)
                        ctx.ReportDiagnostic(Diagnostic.Create(PrefixReturn, location, method.Name));
                }

                if (isPostfix && !method.ReturnsVoid)
                    ctx.ReportDiagnostic(Diagnostic.Create(PostfixReturn, location, method.Name));
            }, SymbolKind.Method);
        });
    }

    private static int GetPatchOptions(IMethodSymbol method, INamedTypeSymbol? options)
    {
        var attribute = FindAttribute(method, options) ?? FindAttribute(method.ContainingType, options);
        return attribute is not null && Argument(attribute, "options")?.Value is int value ? value : 0;
    }

    private static AttributeData? FindAttribute(ISymbol symbol, params INamedTypeSymbol?[] types) =>
        GetAttributes(symbol).FirstOrDefault(a => IsAttribute(a, types));

    // All supported attributes are inherited. Only target selectors and HarmonyPatch allow multiples.
    private static IEnumerable<AttributeData> GetAttributes(ISymbol symbol)
    {
        var nearerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        bool inherited = false;
        for (ISymbol? current = symbol;
             current is not null;
             current = current switch
             {
                 IMethodSymbol method => method.OverriddenMethod,
                 INamedTypeSymbol type => type.BaseType,
                 _ => null,
             })
        {
            var attributes = current.GetAttributes();
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass is not { } type)
                    continue;
                if (!inherited || !nearerTypes.Contains(type) || AllowsMultiple(type))
                    yield return attribute;
            }

            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass is { } type)
                    nearerTypes.Add(type);
            }

            inherited = true;
        }
    }

    private static bool AllowsMultiple(INamedTypeSymbol type) =>
        type.ToDisplayString() is "Disharmony.TargetAttribute" or "Disharmony.TargetsAttribute" or "HarmonyLib.HarmonyPatch";

    private static bool IsAttribute(AttributeData attribute, params INamedTypeSymbol?[] types) =>
        types.Any(type => type is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, type));

    private static bool HasGenericParameters(IMethodSymbol method)
    {
        if (method.Arity != 0)
            return true;
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
        {
            if (type.Arity != 0)
                return true;
        }

        return false;
    }
}
