using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Disharmony.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PatchAnalyzer : DiagnosticAnalyzer
{
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

    public static readonly DiagnosticDescriptor StateWithoutReader = new(
        "DH0030", "State has no reader",
        "State key '{0}' is only bound through out parameters in this patch class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);


    public static readonly DiagnosticDescriptor MultipleParameterBindings = new(
        "DH0016", "Multiple parameter binding attributes", "Parameter '{0}' has multiple binding attributes",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InnerBindingWithoutInnerPatch = new(
        "DH0017", "Parameter binding requires an inner patch",
        "Parameter '{0}' uses __caller or Scope.Inner without an inner patch",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AlwaysRunResultBinding = new(
        "DH0018", "AlwaysRun prefix cannot bind the result",
        "Parameter '{0}' binds the result in an AlwaysRun prefix, which is unsupported",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidExceptionBinding = new(
        "DH0019", "Exception binding requires an AlwaysRun postfix",
        "Parameter '{0}' binds an exception outside an AlwaysRun postfixg",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidDelegateBinding = new(
        "DH0020", "Method binding requires a delegate value",
        "Parameter '{0}' binds a method; use a concrete delegate type such as Action or Func and remove any ref, in, or out modifier",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IncompatibleBindingType = new(
        "DH0021", "Incompatible parameter binding type",
        "Parameter '{0}' cannot bind a value of type '{1}'; use a compatible parameter type and ref/in/out modifier",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor IncompatibleStateTypes = new(
        "DH0022", "Incompatible shared state types",
        "Parameter '{0}' shares state key '{1}' with a parameter of a different type",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConstantBindingUnavailable = new(
        "DH0024", "Inner constant cannot supply this binding",
        "Parameter '{0}' requests an instance, argument, or field from [InnerConstant], which has none",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor VoidPrefixResultBinding = new(
        "DH0025", "Prefix binding the result cannot skip the target",
        "Prefix binds the result through '{0}' but returns void; return bool and return false when supplying a replacement result to skip the target",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReadOnlyPrefixResultBinding = new(
        "DH0026", "Prefix result parameter cannot set the result",
        "Prefix result parameter '{0}' should be declared 'ref' or 'out'",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnknownSpecialParameter = new(
        "DH0027", "Unknown special parameter name",
        "Parameter '{0}' starts with '__' but is not a recognized special name; correct the name or use an explicit binding attribute such as [Parameter]",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateBinding = new(
        "DH0028", "Patch binds the same value more than once",
        "Parameter '{0}' binds the same value as another parameter in this patch",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StateWithoutWriter = new(
        "DH0029", "State has no writer",
        "State key '{0}' has no writer in this patch class; declare a parameter for this key ref or out in a patch that supplies the state",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WrittenValueParameter = new(
        "DH0031", "Patch writes to a parameter passed by value",
        "Writing to parameter '{0}' changes only the patch's local copy; declare it ref or out to update the bound value, or use a local variable for a temporary value",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericMethod = new(
        "DH0001", "Patch method must not contain generic parameters",
        "Patch method '{0}' has generic parameters on the method or a containing type; use a non-generic method in a non-generic type",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor StaticMethod = new(
        "DH0002", "Patch method must be static", "Patch method '{0}' is not static",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PrefixReturn = new(
        "DH0003", "Prefix must return bool or void", "Prefix '{0}' must return bool or void",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PostfixReturn = new(
        "DH0004", "Postfix must return void", "Postfix '{0}' must return void",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AlwaysRunReturn = new(
        "DH0005", "AlwaysRun prefix must return void", "Prefix '{0}' with AlwaysRun must return void",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingPatchClass = new(
        "DH0006", "Patch method requires a discoverable containing class",
        "Patch method '{0}' requires [Patch] or [HarmonyPatch] on its containing class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingTarget = new(
        "DH0007", "Patch method requires a target attribute",
        "Patch method '{0}' requires [Target] or [Targets] on the method or its containing class",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingPatchType = new(
        "DH0008", "Disharmony method attributes require a patch type",
        "Method '{0}' has a Disharmony attribute but no [Prefix] or [Postfix]; add the appropriate patch attribute or remove the unused Disharmony attribute",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultiplePatchTypes = new(
        "DH0009", "Patch method has multiple patch type attributes",
        "Method '{0}' has multiple prefix/postfix attributes",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MultipleInnerTargets = new(
        "DH0010", "Patch method has multiple inner target attributes",
        "Method '{0}' has multiple inner target attributes",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingTargetType = new(
        "DH0011", "Member selector has no declaring type",
        "Selector for patch '{0}' has no declaring type; supply a type or use a qualified member name such as Namespace.Type.Member",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NullInnerConstant = new(
        "DH0012", "Inner constant cannot be null",
        "Patch '{0}' uses [InnerConstant] with null, which is unsupported",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateDiscoveryAttributes = new(
        "DH0014", "Duplicate patch discovery attributes",
        "Class '{0}' has multiple {1} attributes",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingMemberName = new(
        "DH0015", "Member selector requires a name",
        "Selector for patch '{0}' has no member name; supply a name or specify MemberType.Constructor to select a constructor",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AlwaysRunThrow = new(
        "DH0032", "AlwaysRun patch explicitly throws",
        "AlwaysRun patch '{0}' explicitly throws; handle the failure without throwing so other AlwaysRun patches can execute",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);


    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(RegisterChecks);
    }

    private static void RegisterChecks(CompilationStartAnalysisContext start)
    {
        var state = new DiagnosticGenerator((CSharpCompilation)start.Compilation);

        if (!state.CanRun)
            return;

        start.RegisterOperationAction(state.AnalyzeAssignment,
            OperationKind.SimpleAssignment, OperationKind.CompoundAssignment, OperationKind.CoalesceAssignment,
            OperationKind.DeconstructionAssignment, OperationKind.Increment, OperationKind.Decrement, OperationKind.Argument);

        start.RegisterSymbolAction(state.AnalyzeClass, SymbolKind.NamedType);
        start.RegisterSymbolAction(state.AnalyzeMethod, SymbolKind.Method);
        start.RegisterOperationAction(state.AnalyzeThrow, OperationKind.Throw);
    }
}

internal enum ParameterKind
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
