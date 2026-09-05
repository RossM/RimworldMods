using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Disharmony.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PatchMethodAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor GenericMethod = new(
        "DH0001", "Patch method must not contain generic parameters",
        "Patch method '{0}' must not contain generic parameters, including those of its containing types",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    private static readonly DiagnosticDescriptor StaticMethod = new(
        "DH0002", "Patch method must be static", "Patch method '{0}' must be static",
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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(GenericMethod, StaticMethod, PrefixReturn, PostfixReturn, AlwaysRunReturn);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var prefix = start.Compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
            var postfix = start.Compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
            var options = start.Compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
            var flags = start.Compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
            var alwaysRun = flags?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
            if (prefix is null && postfix is null)
                return;

            start.RegisterSymbolAction(ctx =>
            {
                var method = (IMethodSymbol)ctx.Symbol;
                bool isPrefix = FindAttribute(method, prefix) is not null;
                bool isPostfix = FindAttribute(method, postfix) is not null;
                if (!isPrefix && !isPostfix)
                    return;

                var location = method.Locations.FirstOrDefault(l => l.IsInSource);
                if (location is null)
                    return;
                if (HasGenericParameters(method))
                    ctx.ReportDiagnostic(Diagnostic.Create(GenericMethod, location, method.Name));
                if (!method.IsStatic)
                    ctx.ReportDiagnostic(Diagnostic.Create(StaticMethod, location, method.Name));

                // Reflection's ReturnType distinguishes bool from bool&, unlike Roslyn's ReturnType.
                bool returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                                   !method.ReturnsByRef && !method.ReturnsByRefReadonly;
                if (isPrefix)
                {
                    var optionAttribute = FindAttribute(method, options) ??
                                          FindAttribute(method.ContainingType, options);
                    // Custom derived option attributes can compute their value in arbitrary code.
                    // Only the built-in constructor exposes an unambiguous constant option value.
                    bool runsAlways = optionAttribute is not null &&
                                      SymbolEqualityComparer.Default.Equals(optionAttribute.AttributeClass, options) &&
                                      optionAttribute.ConstructorArguments.Length == 1 &&
                                      optionAttribute.ConstructorArguments[0].Value is int value &&
                                      alwaysRun is int mask && (value & mask) != 0;
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

    // Match reflection attribute inheritance without inspecting or executing attribute constructors.
    private static AttributeData? FindAttribute(ISymbol symbol, INamedTypeSymbol? expected)
    {
        bool inherited = false;
        for (ISymbol? current = symbol; current is not null; current = current switch
             {
                 IMethodSymbol method => method.OverriddenMethod,
                 INamedTypeSymbol type => type.BaseType,
                 _ => null,
             })
        {
            foreach (var attribute in current.GetAttributes())
                if (IsAttribute(attribute, expected) && (!inherited || IsInherited(attribute.AttributeClass)))
                    return attribute;
            inherited = true;
        }
        return null;
    }

    private static bool IsInherited(INamedTypeSymbol? attributeType)
    {
        for (var type = attributeType; type is not null; type = type.BaseType)
        {
            var usage = type.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.ToDisplayString() == "System.AttributeUsageAttribute");
            if (usage is not null)
                return !usage.NamedArguments.Any(a => a.Key == "Inherited" && a.Value.Value is false);
        }
        return true;
    }

    private static bool IsAttribute(AttributeData attribute, INamedTypeSymbol? expected)
    {
        if (expected is null)
            return false;
        for (var type = attribute.AttributeClass; type is not null; type = type.BaseType)
            if (SymbolEqualityComparer.Default.Equals(type, expected))
                return true;
        return false;
    }

    private static bool HasGenericParameters(IMethodSymbol method)
    {
        if (method.Arity != 0)
            return true;
        for (var type = method.ContainingType; type is not null; type = type.ContainingType)
            if (type.Arity != 0)
                return true;
        return false;
    }
}
