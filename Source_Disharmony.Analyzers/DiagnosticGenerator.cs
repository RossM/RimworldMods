using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Disharmony.Analyzers;

class DiagnosticGenerator
{
    private readonly CSharpCompilation compilation;
    private readonly INamedTypeSymbol? _PatchAttribute;
    private readonly INamedTypeSymbol? _HarmonyPatch;
    private readonly INamedTypeSymbol? _CategoryAttribute;
    private readonly INamedTypeSymbol? _HarmonyPatchCategory;
    public readonly INamedTypeSymbol? _PrefixAttribute;
    public readonly INamedTypeSymbol? _PostfixAttribute;
    private readonly INamedTypeSymbol? _InnerAttribute;
    private readonly INamedTypeSymbol? _InnerConstantAttribute;
    private readonly INamedTypeSymbol? _TargetAttribute;
    private readonly INamedTypeSymbol? _TargetsAttribute;
    private readonly INamedTypeSymbol? _MemberType;
    private readonly int? _MemberType_Constructor;
    private readonly INamedTypeSymbol? _PatchOptionsAttribute;
    private readonly INamedTypeSymbol? _PatchOptions;
    private readonly int? _PatchOptions_AlwaysRun;
    private readonly INamedTypeSymbol? _Scope;
    private readonly int? _Scope_Inner;
    private readonly int? _Scope_Outer;
    private readonly int? _PatchOptions_AllowUnsafe;
    private readonly INamedTypeSymbol? _Exception;
    private readonly INamedTypeSymbol? _PriorityAttribute;
    private readonly INamedTypeSymbol?[] _methodAttributes;
    private readonly (ParameterKind Kind, INamedTypeSymbol? Type)[] _bindingTypes;

    public DiagnosticGenerator(CSharpCompilation compilation)
    {
        this.compilation = compilation;
        _PatchAttribute = this.compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
        _HarmonyPatch = this.compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatch");
        _CategoryAttribute = this.compilation.GetTypeByMetadataName("Disharmony.CategoryAttribute");
        _HarmonyPatchCategory = this.compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatchCategory");
        _PrefixAttribute = this.compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
        _PostfixAttribute = this.compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
        _InnerAttribute = this.compilation.GetTypeByMetadataName("Disharmony.InnerAttribute");
        _InnerConstantAttribute = this.compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute");
        _TargetAttribute = this.compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
        _TargetsAttribute = this.compilation.GetTypeByMetadataName("Disharmony.TargetsAttribute");
        _MemberType = this.compilation.GetTypeByMetadataName("Disharmony.MemberType");
        _MemberType_Constructor = _MemberType?.GetMembers("Constructor").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _PatchOptionsAttribute = this.compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
        _PatchOptions = this.compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
        _PatchOptions_AlwaysRun = _PatchOptions?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _PatchOptions_AllowUnsafe = _PatchOptions?.GetMembers("AllowUnsafe").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _Scope = this.compilation.GetTypeByMetadataName("Disharmony.Scope");
        _Scope_Inner = _Scope?.GetMembers("Inner").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _Scope_Outer = _Scope?.GetMembers("Outer").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _Exception = this.compilation.GetTypeByMetadataName("System.Exception");
        _PriorityAttribute = this.compilation.GetTypeByMetadataName("Disharmony.PriorityAttribute");

        _methodAttributes =
        [
            _PrefixAttribute, _PostfixAttribute, _TargetAttribute, _TargetsAttribute, _PatchOptionsAttribute, _InnerAttribute,
            _InnerConstantAttribute,
            _PriorityAttribute,
        ];
        _bindingTypes =
        [
            (Kind: ParameterKind.Argument, Type: this.compilation.GetTypeByMetadataName("Disharmony.ParameterAttribute")),
            (Kind: ParameterKind.Instance, Type: this.compilation.GetTypeByMetadataName("Disharmony.InstanceAttribute")),
            (Kind: ParameterKind.Result, Type: this.compilation.GetTypeByMetadataName("Disharmony.ReturnValueAttribute")),
            (Kind: ParameterKind.State, Type: this.compilation.GetTypeByMetadataName("Disharmony.StateAttribute")),
            (Kind: ParameterKind.Field, Type: this.compilation.GetTypeByMetadataName("Disharmony.FieldAttribute")),
            (Kind: ParameterKind.BaseMethod, Type: this.compilation.GetTypeByMetadataName("Disharmony.BaseMethodAttribute")),
            (Kind: ParameterKind.Method, Type: this.compilation.GetTypeByMetadataName("Disharmony.MethodAttribute")),
            (Kind: ParameterKind.Exception, Type: this.compilation.GetTypeByMetadataName("Disharmony.ExceptionAttribute")),
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
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.DuplicateDiscoveryAttributes, typeLocation, type.Name, "[Patch]/[HarmonyPatch]"));
            if (attributes.Count(a => Helpers.IsAttribute(a, _CategoryAttribute) || Helpers.IsAttribute(a, _HarmonyPatchCategory)) > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.DuplicateDiscoveryAttributes, typeLocation, type.Name,
                    "[Category]/[HarmonyPatchCategory]"));
        }

        var states = new Dictionary<string, List<IParameterSymbol>>();
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            bool isPrefix = Helpers.FindAttribute(method, _PrefixAttribute) is not null;
            bool isPostfix = Helpers.FindAttribute(method, _PostfixAttribute) is not null;
            if ((!isPrefix && !isPostfix) || (isPrefix && isPostfix))
                continue;
            var innerAttribute = Helpers.FindAttribute(method, _InnerAttribute, _InnerConstantAttribute) ?? Helpers.FindAttribute(type, _InnerAttribute, _InnerConstantAttribute);
            bool isInner = innerAttribute is not null;
            var patchOptions = Helpers.GetPatchOptions(method, _PatchOptionsAttribute);
            bool alwaysRun = _PatchOptions_AlwaysRun is int mask && (patchOptions & mask) != 0;
            bool allowUnsafe = _PatchOptions_AllowUnsafe is int maskUnsafe && (patchOptions & maskUnsafe) != 0;
            var constantType = innerAttribute is not null && Helpers.IsAttribute(innerAttribute, _InnerConstantAttribute)
                ? Helpers.Argument(innerAttribute, "value")?.Type
                : null;

            var boundValues = new Dictionary<(ParameterKind Kind, int? Scope, object? Selector), List<IParameterSymbol>>();

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
                    if (kind == ParameterKind.Argument && parameter.Name.StartsWith("__"))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.UnknownSpecialParameter, parameterLocation, parameter.Name));
                }
                else
                {
                    kind = _bindingTypes.First(pair => Helpers.IsAttribute(binding, pair.Type)).Kind;
                }

                int? explicitScope = binding is not null ? Helpers.Argument(binding, "scope")?.Value as int? : null;
                bool explicitlyInner = explicitScope is int selected && selected == _Scope_Inner;
                if (!isInner && (kind == ParameterKind.Caller || explicitlyInner))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.InnerBindingWithoutInnerPatch, parameterLocation, parameter.Name));
                    continue;
                }

                // Named arguments and fields retain Any's fallback semantics on inner patches.
                // Index selectors stay distinct from names because equating them needs target metadata.
                var identityKind = kind == ParameterKind.Caller ? ParameterKind.Instance : kind;
                object? selector = kind switch
                {
                    ParameterKind.Argument => binding is null
                        ? parameter.Name
                        : Helpers.Argument(binding, "index")?.Value ?? Helpers.Argument(binding, "name")?.Value ?? parameter.Name,
                    ParameterKind.Field => binding is null
                        ? parameter.Name.Substring(3)
                        : Helpers.Argument(binding, "name")?.Value ?? parameter.Name,
                    ParameterKind.Method => Helpers.Argument(binding!, "name")?.Value ?? parameter.Name,
                    ParameterKind.State => binding is null ? parameter.Name : Helpers.Argument(binding, "key")?.Value ?? parameter.Name,
                    _ => null,
                };
                int? identityScope = kind switch
                {
                    ParameterKind.Result or ParameterKind.Exception or ParameterKind.State or ParameterKind.BaseMethod => null,
                    ParameterKind.Caller => _Scope_Outer,
                    _ when !isInner => _Scope_Outer,
                    ParameterKind.Field => explicitScope ?? 0,
                    ParameterKind.Argument when selector is string => explicitScope ?? 0,
                    _ => explicitScope is null or 0 ? _Scope_Inner : explicitScope,
                };
                var identity = (identityKind, identityScope, selector);
                if (!boundValues.TryGetValue(identity, out var boundParameters))
                    boundValues.Add(identity, boundParameters = []);
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
                            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.VoidPrefixResultBinding, parameterLocation, parameter.Name));
                        if (parameter.RefKind is not (RefKind.Ref or RefKind.Out))
                            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.ReadOnlyPrefixResultBinding, parameterLocation, parameter.Name));
                    }
                }

                if (kind == ParameterKind.Exception)
                {
                    if (!isPostfix || !alwaysRun)
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.InvalidExceptionBinding, parameterLocation, parameter.Name));
                    if (_Exception is not null && !Helpers.CanBindKnownType(compilation, parameter, _Exception, allowUnsafe))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.IncompatibleBindingType, parameterLocation, parameter.Name, "System.Exception"));
                }

                if (kind is ParameterKind.BaseMethod or ParameterKind.Method && (parameter.RefKind != RefKind.None || parameter.Type is not INamedTypeSymbol { DelegateInvokeMethod: not null }))
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.InvalidDelegateBinding, parameterLocation, parameter.Name));

                if (constantType is not null)
                {
                    if (kind == ParameterKind.Result && !Helpers.CanBindKnownType(compilation, parameter, constantType, allowUnsafe))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.IncompatibleBindingType, parameterLocation, parameter.Name, constantType.ToDisplayString()));
                    bool selectsInner = explicitlyInner || explicitScope != _Scope_Outer;
                    bool hasIndex = binding is not null && Helpers.Argument(binding, "index") is not null;
                    if ((kind == ParameterKind.Instance && selectsInner) || (kind == ParameterKind.Argument && (explicitlyInner || (hasIndex && selectsInner))) || (kind == ParameterKind.Field && explicitlyInner))
                        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.ConstantBindingUnavailable, parameterLocation, parameter.Name));
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
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.DuplicateBinding, Helpers.GetLocation(parameter), parameter.Name));
                }
            }
        }

        // Assembly registration groups state by declaring patch class, even across different targets.
        foreach (var state in states)
        {
            if (!state.Value.Any(p => p.RefKind is RefKind.Ref or RefKind.Out))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.StateWithoutWriter, Helpers.GetLocation(parameter), state.Key));
                }

            if (state.Value.All(p => p.RefKind == RefKind.Out))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.StateWithoutReader, Helpers.GetLocation(parameter), state.Key));
                }

            if (state.Value.Skip(1).Any(p => !compilation.ClassifyConversion(state.Value[0].Type, p.Type).IsIdentity))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.IncompatibleStateTypes, Helpers.GetLocation(parameter), parameter.Name, state.Key));
                }
        }
    }

    public void AnalyzeMethod(SymbolAnalysisContext ctx)
    {
        var method = (IMethodSymbol)ctx.Symbol;
        bool isPrefix = Helpers.FindAttribute(method, _PrefixAttribute) is not null;
        bool isPostfix = Helpers.FindAttribute(method, _PostfixAttribute) is not null;
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
                bool runsAlways = _PatchOptions_AlwaysRun is int mask && (Helpers.GetPatchOptions(method, _PatchOptionsAttribute) & mask) != 0;
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
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingTargetType, Helpers.SelectorLocation(selector, location), method.Name));
            CheckSelector(selector, ctx, location, method);
        }

        foreach (var selector in innerTargets)
        {
            if (Helpers.IsAttribute(selector, _InnerConstantAttribute) && Helpers.Argument(selector, "value") is { IsNull: true })
            {
                ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.NullInnerConstant, Helpers.SelectorLocation(selector, location), method.Name));
            }
            else if (Helpers.IsAttribute(selector, _InnerAttribute))
            {
                // Inner selectors do not inherit the outer target's declaring type.
                if (Helpers.HasNoTypeOrQualifiedName(selector))
                    ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingTargetType, Helpers.SelectorLocation(selector, location), method.Name));
                CheckSelector(selector, ctx, location, method);
            }
        }
    }

    public void AnalyzeThrow(OperationAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol is not IMethodSymbol method ||
            Helpers.FindAttribute(method, _PrefixAttribute, _PostfixAttribute) is null ||
            _PatchOptions_AlwaysRun is not int mask || (Helpers.GetPatchOptions(method, _PatchOptionsAttribute) & mask) == 0)
            return;

        // Nested functions have their own execution; following calls is outside this check.
        for (var parent = ctx.Operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return;
        }

        ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.AlwaysRunThrow, ctx.Operation.Syntax.GetLocation(), method.Name));
    }

    public void CheckWrite(IOperation target, OperationAnalysisContext ctx)
    {
        if (target is ITupleOperation tuple)
            foreach (var element in tuple.Elements)
                CheckWrite(element, ctx);
        else if (target is IConversionOperation conversion)
            CheckWrite(conversion.Operand, ctx);
        else if (target is IParameterReferenceOperation reference &&
                 reference.Parameter.RefKind is not (RefKind.Ref or RefKind.Out) &&
                 reference.Parameter.ContainingSymbol is IMethodSymbol method &&
                 (Helpers.FindAttribute(method, _PrefixAttribute) is not null || Helpers.FindAttribute(method, _PostfixAttribute) is not null))
            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.WrittenValueParameter, reference.Syntax.GetLocation(), reference.Parameter.Name));
    }

    public void CheckSelector(AttributeData selector, SymbolAnalysisContext ctx, Location location, IMethodSymbol method)
    {
        var kind = Helpers.Argument(selector, "memberType");
        // Overloads without memberType default to Any (zero).
        int value = kind?.Value is int explicitKind ? explicitKind : 0;
        if (_MemberType_Constructor is int constructor && value != constructor &&
            (Helpers.Argument(selector, "methodName") ?? Helpers.Argument(selector, "memberName")) is null or { IsNull: true })
            ctx.ReportDiagnostic(Diagnostic.Create(PatchAnalyzer.MissingMemberName, Helpers.SelectorLocation(selector, location), method.Name));
    }
}
