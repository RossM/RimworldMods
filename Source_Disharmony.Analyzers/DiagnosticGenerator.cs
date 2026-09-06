using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Disharmony.Analyzers;

using ParameterKey = (ParameterKind identityKind, int? identityScope, object? selector);

internal class DiagnosticGenerator
{
    public bool CanRun => _PrefixAttribute is not null || _PostfixAttribute is not null;

    private readonly CSharpCompilation compilation;
    private readonly INamedTypeSymbol? _PatchAttribute;
    private readonly INamedTypeSymbol? _HarmonyPatch;
    private readonly INamedTypeSymbol? _CategoryAttribute;
    private readonly INamedTypeSymbol? _HarmonyPatchCategory;
    private readonly INamedTypeSymbol? _PrefixAttribute;
    private readonly INamedTypeSymbol? _PostfixAttribute;
    private readonly INamedTypeSymbol? _InnerAttribute;
    private readonly INamedTypeSymbol? _InnerConstantAttribute;
    private readonly INamedTypeSymbol? _TargetAttribute;
    private readonly INamedTypeSymbol? _TargetsAttribute;
    private readonly int _MemberType_Constructor;
    private readonly INamedTypeSymbol? _PatchOptionsAttribute;
    private readonly int _PatchOptions_AlwaysRun;
    private readonly int _Scope_Any;
    private readonly int _Scope_Inner;
    private readonly int _Scope_Outer;
    private readonly int _PatchOptions_AllowUnsafe;
    private readonly INamedTypeSymbol? _Exception;
    private readonly INamedTypeSymbol? _PriorityAttribute;
    private readonly INamedTypeSymbol?[] _methodAttributes;
    private readonly (ParameterKind Kind, INamedTypeSymbol? Type)[] _bindingTypes;

    public DiagnosticGenerator(CSharpCompilation compilation)
    {
        this.compilation = compilation;

        // Attributes

        _PatchAttribute = compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
        _CategoryAttribute = compilation.GetTypeByMetadataName("Disharmony.CategoryAttribute");
        _PrefixAttribute = compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
        _PostfixAttribute = compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
        _InnerAttribute = compilation.GetTypeByMetadataName("Disharmony.InnerAttribute");
        _InnerConstantAttribute = compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute");
        _TargetAttribute = compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
        _TargetsAttribute = compilation.GetTypeByMetadataName("Disharmony.TargetsAttribute");
        _PatchOptionsAttribute = compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
        _PriorityAttribute = compilation.GetTypeByMetadataName("Disharmony.PriorityAttribute");

        // Enums

        INamedTypeSymbol? memberType = compilation.GetTypeByMetadataName("Disharmony.MemberType");
        _MemberType_Constructor = memberType?.GetMembers("Constructor").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int? ?? -1;

        INamedTypeSymbol? patchOptions = compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
        _PatchOptions_AlwaysRun
            = patchOptions?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int? ?? 0;
        _PatchOptions_AllowUnsafe
            = patchOptions?.GetMembers("AllowUnsafe").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int? ?? 0;

        INamedTypeSymbol? scope = compilation.GetTypeByMetadataName("Disharmony.Scope");
        _Scope_Any = scope?.GetMembers("Any").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int? ?? -1;
        _Scope_Inner = scope?.GetMembers("Inner").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int? ?? -1;
        _Scope_Outer = scope?.GetMembers("Outer").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int? ?? -2;

        // Harmony

        _HarmonyPatch = compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatch");
        _HarmonyPatchCategory = compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatchCategory");

        _Exception = compilation.GetTypeByMetadataName("System.Exception");

        _methodAttributes =
        [
            _PrefixAttribute, _PostfixAttribute, _TargetAttribute, _TargetsAttribute, _PatchOptionsAttribute, _InnerAttribute,
            _InnerConstantAttribute,
            _PriorityAttribute,
        ];
        _bindingTypes =
        [
            (Kind: ParameterKind.Argument, Type: compilation.GetTypeByMetadataName("Disharmony.ParameterAttribute")),
            (Kind: ParameterKind.Instance, Type: compilation.GetTypeByMetadataName("Disharmony.InstanceAttribute")),
            (Kind: ParameterKind.Result, Type: compilation.GetTypeByMetadataName("Disharmony.ReturnValueAttribute")),
            (Kind: ParameterKind.State, Type: compilation.GetTypeByMetadataName("Disharmony.StateAttribute")),
            (Kind: ParameterKind.Field, Type: compilation.GetTypeByMetadataName("Disharmony.FieldAttribute")),
            (Kind: ParameterKind.BaseMethod, Type: compilation.GetTypeByMetadataName("Disharmony.BaseMethodAttribute")),
            (Kind: ParameterKind.Method, Type: compilation.GetTypeByMetadataName("Disharmony.MethodAttribute")),
            (Kind: ParameterKind.Exception, Type: compilation.GetTypeByMetadataName("Disharmony.ExceptionAttribute")),
        ];
    }

    public void AnalyzeAssignment(OperationAnalysisContext ctx)
    {
        var target = ctx.Operation switch
        {
            IAssignmentOperation assignment => assignment.Target,
            IIncrementOrDecrementOperation increment => increment.Target,
            IArgumentOperation { Parameter.RefKind: RefKind.Ref or RefKind.Out } argument => argument.Value,
            _ => null,
        };
        if (target is not null)
            CheckWrite(target, ctx);
    }

    public void AnalyzeClass(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;
        if (type.TypeKind != TypeKind.Class)
            return;
        var attributes = Helpers.GetAttributes(type).ToArray();
        var typeLocation = Helpers.GetLocation(type);
        if (typeLocation is not null)
        {
            if (attributes.Count(a => Helpers.IsAttribute(a, _PatchAttribute) || Helpers.IsAttribute(a, _HarmonyPatch)) > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.DuplicateDiscoveryAttributes, typeLocation, type.Name,
                    "[Patch]/[HarmonyPatch]"));
        }

        var states = new Dictionary<string, List<IParameterSymbol>>();
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            bool isPrefix = Helpers.HasAttribute(method, _PrefixAttribute);
            bool isPostfix = Helpers.HasAttribute(method, _PostfixAttribute);
            if ((!isPrefix && !isPostfix) || (isPrefix && isPostfix))
                continue;
            var innerAttribute = Helpers.FindAttribute(method, _InnerAttribute, _InnerConstantAttribute) ??
                                 Helpers.FindAttribute(type, _InnerAttribute, _InnerConstantAttribute);
            bool isInner = innerAttribute is not null;
            var patchOptions = Helpers.GetPatchOptions(method, _PatchOptionsAttribute);
            bool alwaysRun = (patchOptions & _PatchOptions_AlwaysRun) != 0;
            bool allowUnsafe = (patchOptions & _PatchOptions_AllowUnsafe) != 0;
            var constantType = innerAttribute is not null && Helpers.IsAttribute(innerAttribute, _InnerConstantAttribute)
                ? Helpers.Argument(innerAttribute, "value")?.Type
                : null;

            var boundValues = new Dictionary<ParameterKey, List<IParameterSymbol>>();

            foreach (var parameter in method.Parameters)
            {
                var parameterLocation = Helpers.GetLocation(parameter);
                if (parameterLocation is null || parameter.Type.TypeKind == TypeKind.Error)
                    continue;
                var bindings = parameter.GetAttributes().Where(a => _bindingTypes.Any(pair => Helpers.IsAttribute(a, pair.Type))).ToArray();
                if (bindings.Length > 1)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MultipleParameterBindings, parameterLocation, parameter.Name));
                    continue;
                }

                var binding = bindings.SingleOrDefault();
                ParameterKind kind = GetBindingKind(parameter, binding);

                if (binding is null && kind == ParameterKind.Argument && parameter.Name.StartsWith("__"))
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.UnknownSpecialParameter, parameterLocation, parameter.Name));

                int? explicitScope = Helpers.Argument(binding, "scope")?.Value as int?;
                bool explicitlyInner = explicitScope is int selected && selected == _Scope_Inner;
                if (!isInner && (kind == ParameterKind.Caller || explicitlyInner))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.InnerBindingWithoutInnerPatch, parameterLocation, parameter.Name));
                    continue;
                }

                ParameterKey parameterKey = GetParameterKey(parameter, binding, kind, explicitScope, isInner);
                if (!boundValues.TryGetValue(parameterKey, out var boundParameters))
                    boundValues.Add(parameterKey, boundParameters = []);
                boundParameters.Add(parameter);

                if (kind == ParameterKind.Result && isPrefix)
                {
                    if (alwaysRun)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.AlwaysRunResultBinding, parameterLocation, parameter.Name));
                    }
                    else
                    {
                        if (method.ReturnsVoid)
                            ctx.ReportDiagnostic(
                                Diagnostic.Create(PatchAnalyzer.VoidPrefixResultBinding, parameterLocation, parameter.Name));
                        if (parameter.RefKind is not (RefKind.Ref or RefKind.Out))
                            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.ReadOnlyPrefixResultBinding, parameterLocation,
                                parameter.Name));
                    }
                }

                if (kind == ParameterKind.Exception)
                {
                    if (!isPostfix || !alwaysRun)
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.InvalidExceptionBinding, parameterLocation, parameter.Name));
                    if (_Exception is not null && !Helpers.CanBindKnownType(compilation, parameter, _Exception, allowUnsafe))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.IncompatibleBindingType, parameterLocation, parameter.Name,
                            "System.Exception"));
                }

                if (kind is ParameterKind.BaseMethod or ParameterKind.Method && (parameter.RefKind != RefKind.None ||
                                                                                 parameter.Type is not INamedTypeSymbol
                                                                                 {
                                                                                     DelegateInvokeMethod: not null,
                                                                                 }))
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.InvalidDelegateBinding, parameterLocation, parameter.Name));

                if (constantType is not null)
                {
                    if (kind == ParameterKind.Result && !Helpers.CanBindKnownType(compilation, parameter, constantType, allowUnsafe))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.IncompatibleBindingType, parameterLocation, parameter.Name,
                            constantType.ToDisplayString()));
                    bool selectsInner = explicitlyInner || explicitScope != _Scope_Outer;
                    bool hasIndex = binding is not null && Helpers.Argument(binding, "index") is not null;
                    if ((kind == ParameterKind.Instance && selectsInner) ||
                        (kind == ParameterKind.Argument && (explicitlyInner || (hasIndex && selectsInner))) ||
                        (kind == ParameterKind.Field && explicitlyInner))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.ConstantBindingUnavailable, parameterLocation,
                            parameter.Name));
                }

                if (kind == ParameterKind.State)
                {
                    string key = binding is null ? parameter.Name : Helpers.Argument(binding, "key")?.Value as string ?? parameter.Name;
                    if (!states.TryGetValue(key, out var parameters))
                        states.Add(key, parameters = []);
                    parameters.Add(parameter);
                }
            }

            foreach (var parameters in boundValues.Values.Where(parameters => parameters.Count > 1))
            {
                foreach (var parameter in parameters)
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.DuplicateBinding, Helpers.GetLocation(parameter), parameter.Name));
            }
        }

        // Assembly registration groups state by declaring patch class, even across different targets.
        foreach (var state in states)
        {
            if (!state.Value.Any(p => p.RefKind is RefKind.Ref or RefKind.Out))
                foreach (var parameter in state.Value)
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.StateWithoutWriter, Helpers.GetLocation(parameter), state.Key));

            if (state.Value.All(p => p.RefKind == RefKind.Out))
                foreach (var parameter in state.Value)
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.StateWithoutReader, Helpers.GetLocation(parameter), state.Key));

            if (state.Value.Skip(1).Any(p => !compilation.ClassifyConversion(state.Value[0].Type, p.Type).IsIdentity))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.IncompatibleStateTypes, Helpers.GetLocation(parameter),
                        parameter.Name, state.Key));
                }
        }
    }

    private ParameterKey GetParameterKey(
        IParameterSymbol parameter,
        AttributeData? binding,
        ParameterKind kind,
        int? explicitScope,
        bool isInner)
    {
        // Named arguments and fields retain Any's fallback semantics on inner patches.
        // Index selectors stay distinct from names because equating them needs target metadata.

        int? defaultScope = !isInner ? _Scope_Outer : explicitScope is null || explicitScope == _Scope_Any ? _Scope_Inner: explicitScope;
        ParameterKey identity = kind switch
        {
            ParameterKind.Argument => Helpers.Argument(binding, "index") is not null ?
                (ParameterKind.Argument, defaultScope, Helpers.Argument(binding, "index")?.Value) :
                (ParameterKind.Argument, explicitScope ?? _Scope_Any, Helpers.Argument(binding, "name")?.Value ?? parameter.Name),
            ParameterKind.Instance => (ParameterKind.Instance, defaultScope, null),
            ParameterKind.Result => (ParameterKind.Result, null, null),
            ParameterKind.State => (ParameterKind.State, null, Helpers.Argument(binding, "key")?.Value ?? parameter.Name),
            ParameterKind.Field => (ParameterKind.Field, defaultScope, RemoveTripleUnderscore(Helpers.Argument(binding, "name")?.Value as string ?? parameter.Name)),
            ParameterKind.BaseMethod => (ParameterKind.BaseMethod, null, null),
            ParameterKind.Method => (ParameterKind.Method, defaultScope, Helpers.Argument(binding!, "name")?.Value ?? parameter.Name),
            ParameterKind.Exception => (ParameterKind.Exception, null, null),
            ParameterKind.Caller => (ParameterKind.Instance, _Scope_Outer, null),
            _ => throw new ArgumentOutOfRangeException()
        };
        return identity;
    }

    private ParameterKind GetBindingKind(IParameterSymbol parameter, AttributeData? binding)
    {
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
            kind = _bindingTypes.First(pair => Helpers.IsAttribute(binding, pair.Type)).Kind;
        }

        return kind;
    }

    public void AnalyzeMethod(SymbolAnalysisContext ctx)
    {
        var method = (IMethodSymbol)ctx.Symbol;
        bool isPrefix = Helpers.HasAttribute(method, _PrefixAttribute);
        bool isPostfix = Helpers.HasAttribute(method, _PostfixAttribute);
        var location = method.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return;
        if (!isPrefix && !isPostfix)
        {
            // Class defaults, return attributes, and parameter bindings do not mark a helper as a patch.
            if (method.GetAttributes().Any(a => Helpers.IsAttribute(a, _methodAttributes)))
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingPatchType, location, method.Name));
        }
        else
        {
            if (Helpers.FindAttribute(method.ContainingType, _PatchAttribute) is null &&
                Helpers.FindAttribute(method.ContainingType, _HarmonyPatch) is null)
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingPatchClass, location, method.Name));
            if (Helpers.FindAttribute(method, _TargetAttribute, _TargetsAttribute) is null &&
                Helpers.FindAttribute(method.ContainingType, _TargetAttribute, _TargetsAttribute) is null)
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingTarget, location, method.Name));
            if (Helpers.HasGenericParameters(method))
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.GenericMethod, location, method.Name));
            if (!method.IsStatic)
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.StaticMethod, location, method.Name));

            // Reflection's ReturnType distinguishes bool from bool&, unlike Roslyn's ReturnType.
            bool returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                               method is { ReturnsByRef: false, ReturnsByRefReadonly: false };
            if (isPrefix)
            {
                bool runsAlways = (Helpers.GetPatchOptions(method, _PatchOptionsAttribute) & _PatchOptions_AlwaysRun) != 0;
                if (runsAlways && !method.ReturnsVoid)
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.AlwaysRunReturn, location, method.Name));
                else if (!method.ReturnsVoid && !returnsBool)
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.PrefixReturn, location, method.Name));
            }

            if (isPostfix && !method.ReturnsVoid)
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.PostfixReturn, location, method.Name));
        }

        if (method.IsImplicitlyDeclared || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
            return;
        var attributes = Helpers.GetAttributes(method).Concat(Helpers.GetAttributes(method.ContainingType)).ToArray();
        var patchTypes = attributes.Where(a => Helpers.IsAttribute(a, _PrefixAttribute, _PostfixAttribute)).ToArray();
        var innerTargets = attributes.Where(a => Helpers.IsAttribute(a, _InnerAttribute, _InnerConstantAttribute)).ToArray();
        if (patchTypes.Length > 1)
            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MultiplePatchTypes, location, method.Name));
        if (innerTargets.Length > 1)
            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MultipleInnerTargets, location, method.Name));
        if (patchTypes.Length == 0)
            return;

        // Harmony's type-name constructor resolves its declaring type at runtime.
        bool mayHaveDefaultType = attributes.Any(a =>
            (Helpers.IsAttribute(a, _PatchAttribute) && Helpers.Argument(a, "type") is { IsNull: false }) ||
            (Helpers.IsAttribute(a, _HarmonyPatch) &&
             (Helpers.Argument(a, "typeName") is not null || Helpers.Argument(a, "declaringType") is { IsNull: false })));
        foreach (var selector in attributes.Where(a => Helpers.IsAttribute(a, _TargetAttribute, _TargetsAttribute)))
        {
            if (!mayHaveDefaultType && Helpers.HasNoTypeOrQualifiedName(selector))
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingTargetType, Helpers.SelectorLocation(selector, location),
                    method.Name));
            CheckSelector(selector, ctx, location, method);
        }

        foreach (var selector in innerTargets)
        {
            if (Helpers.IsAttribute(selector, _InnerConstantAttribute) && Helpers.Argument(selector, "value") is { IsNull: true })
            {
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.NullInnerConstant, Helpers.SelectorLocation(selector, location),
                    method.Name));
            }
            else if (Helpers.IsAttribute(selector, _InnerAttribute))
            {
                // Inner selectors do not inherit the outer target's declaring type.
                if (Helpers.HasNoTypeOrQualifiedName(selector))
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingTargetType, Helpers.SelectorLocation(selector, location),
                        method.Name));
                CheckSelector(selector, ctx, location, method);
            }
        }
    }

    public void AnalyzeThrow(OperationAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol is not IMethodSymbol method || !IsPatchMethod(method) ||
            (Helpers.GetPatchOptions(method, _PatchOptionsAttribute) & _PatchOptions_AlwaysRun) == 0)
            return;

        // Nested functions have their own execution; following calls is outside this check.
        for (var parent = ctx.Operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return;
        }

        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.AlwaysRunThrow, ctx.Operation.Syntax.GetLocation(), method.Name));
    }

    private void CheckWrite(IOperation target, OperationAnalysisContext ctx)
    {
        switch (target)
        {
            case ITupleOperation tuple:
            {
                foreach (var element in tuple.Elements)
                    CheckWrite(element, ctx);
                break;
            }
            case IConversionOperation conversion:
            {
                CheckWrite(conversion.Operand, ctx);
                break;
            }
            case IParameterReferenceOperation
            {
                Parameter: { RefKind: not (RefKind.Ref or RefKind.Out), ContainingSymbol: IMethodSymbol method },
            } reference when IsPatchMethod(method):
            {
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.WrittenValueParameter, reference.Syntax.GetLocation(),
                    reference.Parameter.Name));
                break;
            }
        }
    }

    private bool IsPatchMethod(IMethodSymbol method) =>
        Helpers.HasAttribute(method, _PrefixAttribute, _PostfixAttribute);

    private void CheckSelector(AttributeData selector, SymbolAnalysisContext ctx, Location location, IMethodSymbol method)
    {
        var kind = Helpers.Argument(selector, "memberType");
        // Overloads without memberType default to Any (zero).
        int value = kind?.Value is int explicitKind ? explicitKind : 0;
        if (_MemberType_Constructor is int constructor && value != constructor &&
            (Helpers.Argument(selector, "methodName") ?? Helpers.Argument(selector, "memberName")) is null or { IsNull: true })
            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingMemberName, Helpers.SelectorLocation(selector, location),
                method.Name));
    }

    private static string RemoveTripleUnderscore(string s)
    {
        return s.StartsWith("___") ? s[3..] : s;
    }
}
