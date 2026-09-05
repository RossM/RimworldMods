using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static Disharmony.Analyzers.AttributeHelpers;

namespace Disharmony.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PatchMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        GenericMethod, StaticMethod, PrefixReturn, PostfixReturn, AlwaysRunReturn, MissingPatchClass, MissingTarget, MissingPatchType,
        MultiplePatchTypes, MultipleInnerTargets, MissingTargetType, NullInnerConstant,
        DuplicateDiscoveryAttributes, MissingMemberName,
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

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(RegisterChecks);
    }

    private static void RegisterChecks(CompilationStartAnalysisContext start)
    {
        var patch = start.Compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
        var harmonyPatch = start.Compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatch");
        var category = start.Compilation.GetTypeByMetadataName("Disharmony.CategoryAttribute");
        var harmonyCategory = start.Compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatchCategory");
        var prefix = start.Compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
        var postfix = start.Compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
        var inner = start.Compilation.GetTypeByMetadataName("Disharmony.InnerAttribute");
        var constant = start.Compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute");
        var target = start.Compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
        var targets = start.Compilation.GetTypeByMetadataName("Disharmony.TargetsAttribute");
        var memberType = start.Compilation.GetTypeByMetadataName("Disharmony.MemberType");
        var constructorKind = memberType?.GetMembers("Constructor").OfType<IFieldSymbol>()
            .FirstOrDefault()?.ConstantValue as int?;

        var options = start.Compilation.GetTypeByMetadataName("Disharmony.PatchOptionsAttribute");
        var flags = start.Compilation.GetTypeByMetadataName("Disharmony.PatchOptions");
        var alwaysRun = flags?.GetMembers("AlwaysRun").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as int?;
        var methodAttributes = new[]
        {
            prefix, postfix, target, targets, options, inner, constant,
            start.Compilation.GetTypeByMetadataName("Disharmony.PriorityAttribute"),
        };
        if (prefix is null && postfix is null)
            return;

        start.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
        start.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);

        void AnalyzeClass(SymbolAnalysisContext ctx)
        {
            var type = (INamedTypeSymbol)ctx.Symbol;
            if (type.TypeKind != TypeKind.Class)
                return;
            var attributes = GetAttributes(type).ToArray();
            var location = type.Locations.FirstOrDefault(l => l.IsInSource);
            if (location is null)
                return;

            if (attributes.Count(a => IsAttribute(a, patch) || IsAttribute(a, harmonyPatch)) > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(DuplicateDiscoveryAttributes, location, type.Name, "[Patch]/[HarmonyPatch]"));
            if (attributes.Count(a => IsAttribute(a, category) || IsAttribute(a, harmonyCategory)) > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(DuplicateDiscoveryAttributes, location, type.Name,
                    "[Category]/[HarmonyPatchCategory]"));
        }

        void AnalyzeMethod(SymbolAnalysisContext ctx)
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
            }
            else
            {
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
            }

            if (method.IsImplicitlyDeclared || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
                return;
            var attributes = GetAttributes(method).Concat(GetAttributes(method.ContainingType)).ToArray();
            var patchTypes = attributes.Where(a => IsAttribute(a, prefix, postfix)).ToArray();
            var innerTargets = attributes.Where(a => IsAttribute(a, inner, constant)).ToArray();
            if (patchTypes.Length > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(MultiplePatchTypes, location, method.Name));
            if (innerTargets.Length > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(MultipleInnerTargets, location, method.Name));
            if (patchTypes.Length == 0)
                return;

            // Harmony's type-name constructor resolves its declaring type at runtime.
            bool mayHaveDefaultType = attributes.Any(a =>
                (IsAttribute(a, patch) && Argument(a, "type") is { IsNull: false }) ||
                (IsAttribute(a, harmonyPatch) &&
                 (Argument(a, "typeName") is not null || Argument(a, "declaringType") is { IsNull: false })));
            foreach (var selector in attributes.Where(a => IsAttribute(a, target, targets)))
            {
                if (!mayHaveDefaultType && HasNoTypeOrQualifiedName(selector))
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingTargetType, SelectorLocation(selector, location), method.Name));
                CheckSelector(selector);
            }

            foreach (var selector in innerTargets)
            {
                if (IsAttribute(selector, constant) && Argument(selector, "value") is { IsNull: true })
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(NullInnerConstant, SelectorLocation(selector, location), method.Name));
                }
                else if (IsAttribute(selector, inner))
                {
                    // Inner selectors do not inherit the outer target's declaring type.
                    if (HasNoTypeOrQualifiedName(selector))
                        ctx.ReportDiagnostic(Diagnostic.Create(MissingTargetType, SelectorLocation(selector, location), method.Name));
                    CheckSelector(selector);
                }
            }

            void CheckSelector(AttributeData selector)
            {
                var kind = Argument(selector, "memberType");
                // Overloads without memberType default to Any (zero).
                int value = kind?.Value is int explicitKind ? explicitKind : 0;
                if (constructorKind is int constructor && value != constructor &&
                    (Argument(selector, "methodName") ?? Argument(selector, "memberName")) is null or { IsNull: true })
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingMemberName, SelectorLocation(selector, location), method.Name));
            }
        }
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
}
