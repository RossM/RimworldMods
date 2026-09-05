using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Disharmony.Analyzers;

public sealed partial class PatchMethodAnalyzer
{
    private static readonly DiagnosticDescriptor MultipleParameterBindings = new(
        "DH0016", "Multiple parameter binding attributes", "Parameter '{0}' has multiple binding attributes; use only one",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InnerBindingWithoutInnerPatch = new(
        "DH0017", "Parameter binding requires an inner patch", "Parameter '{0}' requires an inner patch",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AlwaysRunResultBinding = new(
        "DH0018", "AlwaysRun prefix cannot bind the result", "Parameter '{0}' binds the result in an AlwaysRun prefix",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidExceptionBinding = new(
        "DH0019", "Exception binding requires an AlwaysRun postfix", "Parameter '{0}' can bind an exception only in an AlwaysRun postfix",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidDelegateBinding = new(
        "DH0020", "Method binding requires a delegate value", "Parameter '{0}' must be a concrete delegate type passed by value",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor IncompatibleBindingType = new(
        "DH0021", "Incompatible parameter binding type", "Parameter '{0}' cannot bind '{1}' with its declared type and modifier",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor IncompatibleStateTypes = new(
        "DH0022", "Incompatible shared state types", "Parameter '{0}' uses state key '{1}' with a different type from another parameter in this patch class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConstantBindingUnavailable = new(
        "DH0024", "Inner constant cannot supply this binding", "Parameter '{0}' requests an instance, argument, or field from an inner constant",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor VoidPrefixResultBinding = new(
        "DH0025", "Prefix binding the result cannot skip the target", "Prefix result parameter '{0}' is used in a void prefix; return bool to allow skipping the target",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ReadOnlyPrefixResultBinding = new(
        "DH0026", "Prefix result parameter cannot set the result", "Prefix result parameter '{0}' should be ref or out to set the result",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);
    private enum ParameterKind { Argument, Instance, Result, State, Field, BaseMethod, Method, Exception, Caller }

    private static void RegisterParameterChecks(CompilationStartAnalysisContext start)
    {
        var compilation = (CSharpCompilation)start.Compilation;
        var prefix = compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
        var postfix = compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
        var inner = compilation.GetTypeByMetadataName("Disharmony.InnerAttribute");
        var constant = compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute");
        var options = compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
        var flags = compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
        var scope = compilation.GetTypeByMetadataName("Disharmony.Scope");
        var innerScope = scope?.GetMembers("Inner").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        var outerScope = scope?.GetMembers("Outer").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        var alwaysRunMask = flags?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        var unsafeMask = flags?.GetMembers("AllowUnsafe").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        var bindingTypes = new[]
        {
            (Kind: ParameterKind.Argument, Type: compilation.GetTypeByMetadataName("Disharmony.ParameterAttribute")),
            (Kind: ParameterKind.Instance, Type: compilation.GetTypeByMetadataName("Disharmony.InstanceAttribute")),
            (Kind: ParameterKind.Result, Type: compilation.GetTypeByMetadataName("Disharmony.ReturnValueAttribute")),
            (Kind: ParameterKind.State, Type: compilation.GetTypeByMetadataName("Disharmony.StateAttribute")),
            (Kind: ParameterKind.Field, Type: compilation.GetTypeByMetadataName("Disharmony.FieldAttribute")),
            (Kind: ParameterKind.BaseMethod, Type: compilation.GetTypeByMetadataName("Disharmony.BaseMethodAttribute")),
            (Kind: ParameterKind.Method, Type: compilation.GetTypeByMetadataName("Disharmony.MethodAttribute")),
            (Kind: ParameterKind.Exception, Type: compilation.GetTypeByMetadataName("Disharmony.ExceptionAttribute")),
        };

        start.RegisterSymbolAction(ctx =>
        {
            var type = (INamedTypeSymbol)ctx.Symbol;
            var states = new Dictionary<string, List<IParameterSymbol>>();
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
            {
                bool isPrefix = FindAttribute(method, prefix) is not null;
                bool isPostfix = FindAttribute(method, postfix) is not null;
                if ((!isPrefix && !isPostfix) || (isPrefix && isPostfix))
                    continue;
                var innerAttribute = FindAttribute(method, inner, constant) ?? FindAttribute(type, inner, constant);
                bool isInner = innerAttribute is not null;
                var patchOptions = GetPatchOptions(method, options);
                bool alwaysRun = alwaysRunMask is int mask && (patchOptions & mask) != 0;
                bool allowUnsafe = unsafeMask is int maskUnsafe && (patchOptions & maskUnsafe) != 0;
                var constantType = innerAttribute is not null && IsAttribute(innerAttribute, constant)
                    ? Argument(innerAttribute, "value")?.Type : null;

                foreach (var parameter in method.Parameters)
                {
                    var location = parameter.Locations.FirstOrDefault(l => l.IsInSource);
                    if (location is null || parameter.Type.TypeKind == TypeKind.Error)
                        continue;
                    var bindings = parameter.GetAttributes().Where(a => bindingTypes.Any(pair => IsAttribute(a, pair.Type))).ToArray();
                    if (bindings.Length > 1)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(MultipleParameterBindings, location, parameter.Name));
                        continue;
                    }
                    var binding = bindings.SingleOrDefault();
                    ParameterKind kind;
                    if (binding is null)
                    {
                        kind = parameter.Name switch
                        {
                            "__caller" => ParameterKind.Caller,
                            "__instance" => ParameterKind.Instance,
                            "__result" => ParameterKind.Result,
                            "__state" => ParameterKind.State,
                            "__base" => ParameterKind.BaseMethod,
                            "__exception" => ParameterKind.Exception,
                            _ when parameter.Name.StartsWith("___") => ParameterKind.Field,
                            _ => ParameterKind.Argument,
                        };
                    }
                    else
                    {
                        kind = bindingTypes.First(pair => IsAttribute(binding, pair.Type)).Kind;
                    }

                    int? explicitScope = binding is not null ? Argument(binding, "scope")?.Value as int? : null;
                    bool explicitlyInner = explicitScope is int selected && selected == innerScope;
                    if (!isInner && (kind == ParameterKind.Caller || explicitlyInner))
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(InnerBindingWithoutInnerPatch, location, parameter.Name));
                        continue;
                    }
                    if (kind == ParameterKind.Result && isPrefix)
                    {
                        if (alwaysRun)
                            ctx.ReportDiagnostic(Diagnostic.Create(AlwaysRunResultBinding, location, parameter.Name));
                        else
                        {
                            if (method.ReturnsVoid)
                                ctx.ReportDiagnostic(Diagnostic.Create(VoidPrefixResultBinding, location, parameter.Name));
                            if (parameter.RefKind is not (RefKind.Ref or RefKind.Out))
                                ctx.ReportDiagnostic(Diagnostic.Create(ReadOnlyPrefixResultBinding, location, parameter.Name));
                        }
                    }
                    if (kind == ParameterKind.Exception)
                    {
                        if (!isPostfix || !alwaysRun)
                            ctx.ReportDiagnostic(Diagnostic.Create(InvalidExceptionBinding, location, parameter.Name));
                        if (exceptionType is not null && !CanBindKnownType(compilation, parameter, exceptionType, allowUnsafe))
                            ctx.ReportDiagnostic(Diagnostic.Create(IncompatibleBindingType, location, parameter.Name, "System.Exception"));
                    }
                    if (kind is ParameterKind.BaseMethod or ParameterKind.Method &&
                        (parameter.RefKind != RefKind.None || parameter.Type is not INamedTypeSymbol { DelegateInvokeMethod: not null }))
                        ctx.ReportDiagnostic(Diagnostic.Create(InvalidDelegateBinding, location, parameter.Name));

                    if (constantType is not null)
                    {
                        if (kind == ParameterKind.Result && !CanBindKnownType(compilation, parameter, constantType, allowUnsafe))
                            ctx.ReportDiagnostic(Diagnostic.Create(IncompatibleBindingType, location, parameter.Name, constantType.ToDisplayString()));
                        bool selectsInner = explicitlyInner || explicitScope != outerScope;
                        bool hasIndex = binding is not null && Argument(binding, "index") is not null;
                        if ((kind == ParameterKind.Instance && selectsInner) ||
                             (kind == ParameterKind.Argument && (explicitlyInner || (hasIndex && selectsInner))) ||
                             (kind == ParameterKind.Field && explicitlyInner))
                            ctx.ReportDiagnostic(Diagnostic.Create(ConstantBindingUnavailable, location, parameter.Name));
                    }

                    if (kind == ParameterKind.State)
                    {
                        string key = binding is null ? parameter.Name : Argument(binding, "key")?.Value as string ?? parameter.Name;
                        if (!states.TryGetValue(key, out var parameters))
                            states.Add(key, parameters = new List<IParameterSymbol>());
                        parameters.Add(parameter);
                    }
                }
            }

            // Assembly registration groups state by declaring patch class, even across different targets.
            foreach (var state in states)
            {
                if (state.Value.Skip(1).Any(p => !compilation.ClassifyConversion(state.Value[0].Type, p.Type).IsIdentity))
                {
                    foreach (var parameter in state.Value)
                        ctx.ReportDiagnostic(Diagnostic.Create(IncompatibleStateTypes, parameter.Locations.First(l => l.IsInSource), parameter.Name, state.Key));
                }
            }
        }, SymbolKind.NamedType);
    }

    private static bool CanBindKnownType(CSharpCompilation compilation, IParameterSymbol parameter, ITypeSymbol source, bool allowUnsafe)
    {
        if (allowUnsafe && !parameter.Type.IsValueType && !source.IsValueType)
            return true;
        var destination = parameter.Type;
        if ((parameter.RefKind != RefKind.None && source.IsValueType) || parameter.RefKind == RefKind.Ref)
            return compilation.ClassifyConversion(source, destination).IsIdentity;
        if (parameter.RefKind == RefKind.Out)
            (source, destination) = (destination, source);
        var conversion = compilation.ClassifyConversion(source, destination);
        // Type.IsAssignableFrom permits identity, reference conversions and boxing, but not numeric
        // conversions or user-defined operators. 'in' reads; 'out' writes in the opposite direction.
        return conversion.IsIdentity || conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing);
    }
}
