using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Disharmony.Analyzers.AttributeHelpers;

namespace Disharmony.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PatchAnalyzer : DiagnosticAnalyzer
{
    private enum ParameterKind
    {
        Argument,
        Instance,
        Result,
        State,
        Field,
        BaseMethod,
        Method,
        Exception,
        Caller,
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        GenericMethod, StaticMethod, PrefixReturn, PostfixReturn, AlwaysRunReturn, MissingPatchClass, MissingTarget, MissingPatchType,
        MultiplePatchTypes, MultipleInnerTargets, MissingTargetType, NullInnerConstant,
        DuplicateDiscoveryAttributes, MissingMemberName, AlwaysRunThrow,
        MultipleParameterBindings, InnerBindingWithoutInnerPatch, AlwaysRunResultBinding, InvalidExceptionBinding,
        InvalidDelegateBinding, IncompatibleBindingType, IncompatibleStateTypes, ConstantBindingUnavailable,
        VoidPrefixResultBinding, ReadOnlyPrefixResultBinding, UnknownSpecialParameter, DuplicateBinding, StateWithoutWriter,
        StateWithoutReader, WrittenValueParameter,
    ];

    private static readonly DiagnosticDescriptor StateWithoutReader = new(
        "DH0030", "State has no reader",
        "State key '{0}' is only bound through out parameters in this patch class; add a value, in, or ref binding that consumes the state, correct the state key, or remove the unused state",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);


    private static readonly DiagnosticDescriptor MultipleParameterBindings = new(
        "DH0016", "Multiple parameter binding attributes", "Parameter '{0}' has multiple binding attributes; use only one",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InnerBindingWithoutInnerPatch = new(
        "DH0017", "Parameter binding requires an inner patch",
        "Parameter '{0}' uses __caller or Scope.Inner without an inner patch; add [Inner]/[InnerConstant] or change the parameter binding",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AlwaysRunResultBinding = new(
        "DH0018", "AlwaysRun prefix cannot bind the result",
        "Parameter '{0}' binds the result in an AlwaysRun prefix, which is unsupported; remove the result binding or remove AlwaysRun",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidExceptionBinding = new(
        "DH0019", "Exception binding requires an AlwaysRun postfix",
        "Parameter '{0}' binds an exception outside an AlwaysRun postfix; use [Postfix] with PatchOptions.AlwaysRun or remove the exception binding",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidDelegateBinding = new(
        "DH0020", "Method binding requires a delegate value",
        "Parameter '{0}' binds a method; use a concrete delegate type such as Action or Func and remove any ref, in, or out modifier",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor IncompatibleBindingType = new(
        "DH0021", "Incompatible parameter binding type",
        "Parameter '{0}' cannot bind a value of type '{1}'; use a compatible parameter type and ref/in/out modifier",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor IncompatibleStateTypes = new(
        "DH0022", "Incompatible shared state types",
        "Parameter '{0}' shares state key '{1}' with a parameter of a different type; use the same type for this key or choose a different state key",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConstantBindingUnavailable = new(
        "DH0024", "Inner constant cannot supply this binding",
        "Parameter '{0}' requests an instance, argument, or field from [InnerConstant], which has none; use Scope.Outer to bind from the outer target or remove the parameter",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor VoidPrefixResultBinding = new(
        "DH0025", "Prefix binding the result cannot skip the target",
        "Prefix binds the result through '{0}' but returns void; return bool and return false when supplying a replacement result to skip the target",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ReadOnlyPrefixResultBinding = new(
        "DH0026", "Prefix result parameter cannot set the result",
        "Prefix result parameter '{0}' cannot set the result; declare it ref or out",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnknownSpecialParameter = new(
        "DH0027", "Unknown special parameter name",
        "Parameter '{0}' starts with '__' but is not a recognized special name; correct the name or use an explicit binding attribute such as [Parameter]",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateBinding = new(
        "DH0028", "Patch binds the same value more than once",
        "Parameter '{0}' binds the same value as another parameter in this patch; remove the duplicate parameter or change its binding",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor StateWithoutWriter = new(
        "DH0029", "State has no writer",
        "State key '{0}' has no writer in this patch class; declare a parameter for this key ref or out in a patch that supplies the state",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor WrittenValueParameter = new(
        "DH0031", "Patch writes to a parameter passed by value",
        "Writing to parameter '{0}' changes only the patch's local copy; declare it ref or out to update the bound value, or use a local variable for a temporary value",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

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

    private static readonly DiagnosticDescriptor MultiplePatchTypes = new(
        "DH0009", "Patch method has multiple patch type attributes",
        "Method '{0}' has multiple prefix/postfix attributes; keep exactly one [Prefix] or [Postfix], including inherited attributes",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MultipleInnerTargets = new(
        "DH0010", "Patch method has multiple inner target attributes",
        "Method '{0}' has multiple inner target attributes; keep only one [Inner] or [InnerConstant] across the method and its class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingTargetType = new(
        "DH0011", "Member selector has no declaring type",
        "Selector for patch '{0}' has no declaring type; supply a type or use a qualified member name such as Namespace.Type.Member",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NullInnerConstant = new(
        "DH0012", "Inner constant cannot be null",
        "Patch '{0}' uses [InnerConstant] with null, which is unsupported; supply a non-null constant",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateDiscoveryAttributes = new(
        "DH0014", "Duplicate patch discovery attributes",
        "Class '{0}' has multiple {1} attributes; use only one",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingMemberName = new(
        "DH0015", "Member selector requires a name",
        "Selector for patch '{0}' has no member name; supply a name or specify MemberType.Constructor to select a constructor",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor AlwaysRunThrow = new(
        "DH0032", "AlwaysRun patch explicitly throws",
        "AlwaysRun patch '{0}' explicitly throws; handle the failure without throwing so other AlwaysRun patches can execute",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private CSharpCompilation compilation;
    private INamedTypeSymbol? _PatchAttribute;
    private INamedTypeSymbol? _HarmonyPatch;
    private INamedTypeSymbol? _CategoryAttribute;
    private INamedTypeSymbol? _HarmonyPatchCategory;
    private INamedTypeSymbol? _PrefixAttribute;
    private INamedTypeSymbol? _PostfixAttribute;
    private INamedTypeSymbol? _InnerAttribute;
    private INamedTypeSymbol? _InnerConstantAttribute;
    private INamedTypeSymbol? _TargetAttribute;
    private INamedTypeSymbol? _TargetsAttribute;
    private INamedTypeSymbol? _MemberType;
    private int? _MemberType_Constructor;
    private INamedTypeSymbol? _PatchOptionsAttribute;
    private INamedTypeSymbol? _PatchOptions;
    private int? _PatchOptions_AlwaysRun;
    private INamedTypeSymbol? _Scope;
    private int? _Scope_Inner;
    private int? _Scope_Outer;
    private int? _PatchOptions_AllowUnsafe;
    private INamedTypeSymbol? _Exception;
    private INamedTypeSymbol? _PriorityAttribute;
    private INamedTypeSymbol?[] _methodAttributes;
    private (ParameterKind Kind, INamedTypeSymbol? Type)[] _bindingTypes;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(RegisterChecks);
    }

    private void RegisterChecks(CompilationStartAnalysisContext start)
    {
        compilation = (CSharpCompilation)start.Compilation;
        _PatchAttribute = compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
        _HarmonyPatch = compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatch");
        _CategoryAttribute = compilation.GetTypeByMetadataName("Disharmony.CategoryAttribute");
        _HarmonyPatchCategory = compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatchCategory");
        _PrefixAttribute = compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
        _PostfixAttribute = compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
        _InnerAttribute = compilation.GetTypeByMetadataName("Disharmony.InnerAttribute");
        _InnerConstantAttribute = compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute");
        _TargetAttribute = compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
        _TargetsAttribute = compilation.GetTypeByMetadataName("Disharmony.TargetsAttribute");
        _MemberType = compilation.GetTypeByMetadataName("Disharmony.MemberType");
        _MemberType_Constructor = _MemberType?.GetMembers("Constructor").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _PatchOptionsAttribute = compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
        _PatchOptions = compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
        _PatchOptions_AlwaysRun = _PatchOptions?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _PatchOptions_AllowUnsafe = _PatchOptions?.GetMembers("AllowUnsafe").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _Scope = compilation.GetTypeByMetadataName("Disharmony.Scope");
        _Scope_Inner = _Scope?.GetMembers("Inner").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _Scope_Outer = _Scope?.GetMembers("Outer").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        _Exception = compilation.GetTypeByMetadataName("System.Exception");
        _PriorityAttribute = compilation.GetTypeByMetadataName("Disharmony.PriorityAttribute");

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


        if (_PrefixAttribute is null && _PostfixAttribute is null)
            return;

        start.RegisterOperationAction(AnalyzeAssignment, OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.CoalesceAssignment,
            OperationKind.DeconstructionAssignment, OperationKind.Increment, OperationKind.Decrement, OperationKind.Argument);

        start.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
        start.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        start.RegisterOperationAction(AnalyzeThrow, OperationKind.Throw);
    }

    private void AnalyzeAssignment(OperationAnalysisContext ctx)
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

    private void CheckWrite(IOperation target, OperationAnalysisContext ctx)
    {
        if (target is ITupleOperation tuple)
            foreach (var element in tuple.Elements)
                CheckWrite(element, ctx);
        else if (target is IConversionOperation conversion)
            CheckWrite(conversion.Operand, ctx);
        else if (target is IParameterReferenceOperation reference &&
                 reference.Parameter.RefKind is not (RefKind.Ref or RefKind.Out) &&
                 reference.Parameter.ContainingSymbol is IMethodSymbol method &&
                 (FindAttribute(method, _PrefixAttribute) is not null || FindAttribute(method, _PostfixAttribute) is not null))
            ctx.ReportDiagnostic(Diagnostic.Create(WrittenValueParameter, reference.Syntax.GetLocation(), reference.Parameter.Name));
    }

    private void AnalyzeThrow(OperationAnalysisContext ctx)
    {
        if (ctx.ContainingSymbol is not IMethodSymbol method ||
            FindAttribute(method, _PrefixAttribute, _PostfixAttribute) is null ||
            _PatchOptions_AlwaysRun is not int mask || (GetPatchOptions(method, _PatchOptionsAttribute) & mask) == 0)
            return;

        // Nested functions have their own execution; following calls is outside this check.
        for (var parent = ctx.Operation.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                return;
        }

        ctx.ReportDiagnostic(Diagnostic.Create(AlwaysRunThrow, ctx.Operation.Syntax.GetLocation(), method.Name));
    }

    private void AnalyzeClass(SymbolAnalysisContext ctx)
    {
        var type = (INamedTypeSymbol)ctx.Symbol;
        if (type.TypeKind != TypeKind.Class)
            return;
        var attributes = GetAttributes(type).ToArray();
        var typeLocation = GetLocation(type);
        if (typeLocation is not null)
        {
            if (attributes.Count(a => IsAttribute(a, _PatchAttribute) || IsAttribute(a, _HarmonyPatch)) > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(DuplicateDiscoveryAttributes, typeLocation, type.Name, "[Patch]/[HarmonyPatch]"));
            if (attributes.Count(a => IsAttribute(a, _CategoryAttribute) || IsAttribute(a, _HarmonyPatchCategory)) > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(DuplicateDiscoveryAttributes, typeLocation, type.Name,
                    "[Category]/[HarmonyPatchCategory]"));
        }

        var states = new Dictionary<string, List<IParameterSymbol>>();
        foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
        {
            bool isPrefix = FindAttribute(method, _PrefixAttribute) is not null;
            bool isPostfix = FindAttribute(method, _PostfixAttribute) is not null;
            if ((!isPrefix && !isPostfix) || (isPrefix && isPostfix))
                continue;
            var innerAttribute = FindAttribute(method, _InnerAttribute, _InnerConstantAttribute) ?? FindAttribute(type, _InnerAttribute, _InnerConstantAttribute);
            bool isInner = innerAttribute is not null;
            var patchOptions = GetPatchOptions(method, _PatchOptionsAttribute);
            bool alwaysRun = _PatchOptions_AlwaysRun is int mask && (patchOptions & mask) != 0;
            bool allowUnsafe = _PatchOptions_AllowUnsafe is int maskUnsafe && (patchOptions & maskUnsafe) != 0;
            var constantType = innerAttribute is not null && IsAttribute(innerAttribute, _InnerConstantAttribute)
                ? Argument(innerAttribute, "value")?.Type
                : null;

            var boundValues = new Dictionary<(ParameterKind Kind, int? Scope, object? Selector), List<IParameterSymbol>>();

            foreach (var parameter in method.Parameters)
            {
                var parameterLocation = GetLocation(parameter);
                if (parameterLocation is null || parameter.Type.TypeKind == TypeKind.Error)
                    continue;
                var bindings = parameter.GetAttributes().Where(a => _bindingTypes.Any(pair => IsAttribute(a, pair.Type))).ToArray();
                if (bindings.Length > 1)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(MultipleParameterBindings, parameterLocation, parameter.Name));
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
                        ctx.ReportDiagnostic(Diagnostic.Create(UnknownSpecialParameter, parameterLocation, parameter.Name));
                }
                else
                {
                    kind = _bindingTypes.First(pair => IsAttribute(binding, pair.Type)).Kind;
                }

                int? explicitScope = binding is not null ? Argument(binding, "scope")?.Value as int? : null;
                bool explicitlyInner = explicitScope is int selected && selected == _Scope_Inner;
                if (!isInner && (kind == ParameterKind.Caller || explicitlyInner))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(InnerBindingWithoutInnerPatch, parameterLocation, parameter.Name));
                    continue;
                }

                // Named arguments and fields retain Any's fallback semantics on inner patches.
                // Index selectors stay distinct from names because equating them needs target metadata.
                var identityKind = kind == ParameterKind.Caller ? ParameterKind.Instance : kind;
                object? selector = kind switch
                {
                    ParameterKind.Argument => binding is null
                        ? parameter.Name
                        : Argument(binding, "index")?.Value ?? Argument(binding, "name")?.Value ?? parameter.Name,
                    ParameterKind.Field => binding is null
                        ? parameter.Name.Substring(3)
                        : Argument(binding, "name")?.Value ?? parameter.Name,
                    ParameterKind.Method => Argument(binding!, "name")?.Value ?? parameter.Name,
                    ParameterKind.State => binding is null ? parameter.Name : Argument(binding, "key")?.Value ?? parameter.Name,
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
                        ctx.ReportDiagnostic(Diagnostic.Create(AlwaysRunResultBinding, parameterLocation, parameter.Name));
                    }
                    else
                    {
                        if (method.ReturnsVoid)
                            ctx.ReportDiagnostic(Diagnostic.Create(VoidPrefixResultBinding, parameterLocation, parameter.Name));
                        if (parameter.RefKind is not (RefKind.Ref or RefKind.Out))
                            ctx.ReportDiagnostic(Diagnostic.Create(ReadOnlyPrefixResultBinding, parameterLocation, parameter.Name));
                    }
                }

                if (kind == ParameterKind.Exception)
                {
                    if (!isPostfix || !alwaysRun)
                        ctx.ReportDiagnostic(Diagnostic.Create(InvalidExceptionBinding, parameterLocation, parameter.Name));
                    if (_Exception is not null && !CanBindKnownType(compilation, parameter, _Exception, allowUnsafe))
                        ctx.ReportDiagnostic(Diagnostic.Create(IncompatibleBindingType, parameterLocation, parameter.Name, "System.Exception"));
                }

                if (kind is ParameterKind.BaseMethod or ParameterKind.Method && (parameter.RefKind != RefKind.None || parameter.Type is not INamedTypeSymbol { DelegateInvokeMethod: not null }))
                    ctx.ReportDiagnostic(Diagnostic.Create(InvalidDelegateBinding, parameterLocation, parameter.Name));

                if (constantType is not null)
                {
                    if (kind == ParameterKind.Result && !CanBindKnownType(compilation, parameter, constantType, allowUnsafe))
                        ctx.ReportDiagnostic(Diagnostic.Create(IncompatibleBindingType, parameterLocation, parameter.Name, constantType.ToDisplayString()));
                    bool selectsInner = explicitlyInner || explicitScope != _Scope_Outer;
                    bool hasIndex = binding is not null && Argument(binding, "index") is not null;
                    if ((kind == ParameterKind.Instance && selectsInner) || (kind == ParameterKind.Argument && (explicitlyInner || (hasIndex && selectsInner))) || (kind == ParameterKind.Field && explicitlyInner))
                        ctx.ReportDiagnostic(Diagnostic.Create(ConstantBindingUnavailable, parameterLocation, parameter.Name));
                }

                if (kind == ParameterKind.State)
                {
                    string key = binding is null ? parameter.Name : Argument(binding, "key")?.Value as string ?? parameter.Name;
                    if (!states.TryGetValue(key, out var parameters))
                        states.Add(key, parameters = []);
                    parameters.Add(parameter);
                }
            }

            foreach (var parameters in boundValues.Values.Where(parameters => parameters.Count > 1))
            {
                foreach (var parameter in parameters)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DuplicateBinding, GetLocation(parameter), parameter.Name));
                }
            }
        }

        // Assembly registration groups state by declaring patch class, even across different targets.
        foreach (var state in states)
        {
            if (!state.Value.Any(p => p.RefKind is RefKind.Ref or RefKind.Out))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(StateWithoutWriter, GetLocation(parameter), state.Key));
                }

            if (state.Value.All(p => p.RefKind == RefKind.Out))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(StateWithoutReader, GetLocation(parameter), state.Key));
                }

            if (state.Value.Skip(1).Any(p => !compilation.ClassifyConversion(state.Value[0].Type, p.Type).IsIdentity))
                foreach (var parameter in state.Value)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(IncompatibleStateTypes, GetLocation(parameter), parameter.Name, state.Key));
                }
        }
    }

    private static Location? GetLocation(ISymbol type)
    {
        return type.Locations.FirstOrDefault(l => l.IsInSource);
    }

    private void AnalyzeMethod(SymbolAnalysisContext ctx)
    {
        var method = (IMethodSymbol)ctx.Symbol;
        bool isPrefix = FindAttribute(method, _PrefixAttribute) is not null;
        bool isPostfix = FindAttribute(method, _PostfixAttribute) is not null;
        var location = method.Locations.FirstOrDefault(l => l.IsInSource);
        if (location is null)
            return;
        if (!isPrefix && !isPostfix)
        {
            // Class defaults, return attributes, and parameter bindings do not mark a helper as a patch.
            if (method.GetAttributes().Any(a => IsAttribute(a, _methodAttributes)))
                ctx.ReportDiagnostic(Diagnostic.Create(MissingPatchType, location, method.Name));
        }
        else
        {
            if (FindAttribute(method.ContainingType, _PatchAttribute) is null &&
                FindAttribute(method.ContainingType, _HarmonyPatch) is null)
                ctx.ReportDiagnostic(Diagnostic.Create(MissingPatchClass, location, method.Name));
            if (FindAttribute(method, _TargetAttribute, _TargetsAttribute) is null &&
                FindAttribute(method.ContainingType, _TargetAttribute, _TargetsAttribute) is null)
                ctx.ReportDiagnostic(Diagnostic.Create(MissingTarget, location, method.Name));
            if (HasGenericParameters(method))
                ctx.ReportDiagnostic(Diagnostic.Create(GenericMethod, location, method.Name));
            if (!method.IsStatic)
                ctx.ReportDiagnostic(Diagnostic.Create(StaticMethod, location, method.Name));

            // Reflection's ReturnType distinguishes bool from bool&, unlike Roslyn's ReturnType.
            bool returnsBool = method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                               method is { ReturnsByRef: false, ReturnsByRefReadonly: false };
            if (isPrefix)
            {
                bool runsAlways = _PatchOptions_AlwaysRun is int mask && (GetPatchOptions(method, _PatchOptionsAttribute) & mask) != 0;
                if (runsAlways && !method.ReturnsVoid)
                    ctx.ReportDiagnostic(Diagnostic.Create(AlwaysRunReturn, location, method.Name));
                else if (!method.ReturnsVoid && !returnsBool)
                    ctx.ReportDiagnostic(Diagnostic.Create(PrefixReturn, location, method.Name));
            }

            if (isPostfix && !method.ReturnsVoid)
                ctx.ReportDiagnostic(Diagnostic.Create(PostfixReturn, location, method.Name));
        }

        if (method.IsImplicitlyDeclared || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
            return;
        var attributes = GetAttributes(method).Concat(GetAttributes(method.ContainingType)).ToArray();
        var patchTypes = attributes.Where(a => IsAttribute(a, _PrefixAttribute, _PostfixAttribute)).ToArray();
        var innerTargets = attributes.Where(a => IsAttribute(a, _InnerAttribute, _InnerConstantAttribute)).ToArray();
        if (patchTypes.Length > 1)
            ctx.ReportDiagnostic(Diagnostic.Create(MultiplePatchTypes, location, method.Name));
        if (innerTargets.Length > 1)
            ctx.ReportDiagnostic(Diagnostic.Create(MultipleInnerTargets, location, method.Name));
        if (patchTypes.Length == 0)
            return;

        // Harmony's type-name constructor resolves its declaring type at runtime.
        bool mayHaveDefaultType = attributes.Any(a =>
            (IsAttribute(a, _PatchAttribute) && Argument(a, "type") is { IsNull: false }) ||
            (IsAttribute(a, _HarmonyPatch) &&
             (Argument(a, "typeName") is not null || Argument(a, "declaringType") is { IsNull: false })));
        foreach (var selector in attributes.Where(a => IsAttribute(a, _TargetAttribute, _TargetsAttribute)))
        {
            if (!mayHaveDefaultType && HasNoTypeOrQualifiedName(selector))
                ctx.ReportDiagnostic(Diagnostic.Create(MissingTargetType, SelectorLocation(selector, location), method.Name));
            CheckSelector(selector, ctx, location, method);
        }

        foreach (var selector in innerTargets)
        {
            if (IsAttribute(selector, _InnerConstantAttribute) && Argument(selector, "value") is { IsNull: true })
            {
                ctx.ReportDiagnostic(Diagnostic.Create(NullInnerConstant, SelectorLocation(selector, location), method.Name));
            }
            else if (IsAttribute(selector, _InnerAttribute))
            {
                // Inner selectors do not inherit the outer target's declaring type.
                if (HasNoTypeOrQualifiedName(selector))
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingTargetType, SelectorLocation(selector, location), method.Name));
                CheckSelector(selector, ctx, location, method);
            }
        }
    }

    private void CheckSelector(AttributeData selector, SymbolAnalysisContext ctx, Location? location, IMethodSymbol? method)
    {
        var kind = Argument(selector, "memberType");
        // Overloads without memberType default to Any (zero).
        int value = kind?.Value is int explicitKind ? explicitKind : 0;
        if (_MemberType_Constructor is int constructor && value != constructor &&
            (Argument(selector, "methodName") ?? Argument(selector, "memberName")) is null or { IsNull: true })
            ctx.ReportDiagnostic(Diagnostic.Create(MissingMemberName, SelectorLocation(selector, location), method.Name));
    }

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

    private static bool HasNoTypeOrQualifiedName(AttributeData selector)
    {
        if (Argument(selector, "type") is { IsNull: false })
            return false;
        if ((Argument(selector, "methodName") ?? Argument(selector, "memberName"))?.Value is not string name)
            return true;
        // Both Type:Member and Namespace.Type.Member are resolved using loaded assemblies at runtime.
        return name.IndexOf(':') < 0 && name.IndexOf('.') < 0;
    }

    private static Location SelectorLocation(AttributeData attribute, Location fallback) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback;

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
        return conversion.IsIdentity || (conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing));
    }
}
