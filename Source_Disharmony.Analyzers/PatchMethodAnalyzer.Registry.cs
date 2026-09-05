using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Disharmony.Analyzers;

public sealed partial class PatchMethodAnalyzer
{
    private static readonly DiagnosticDescriptor MultiplePatchTypes = new(
        "DH0009", "Patch method has multiple patch type attributes",
        "Method '{0}' has multiple PatchType attributes; exactly one is supported",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MultipleInnerTargets = new(
        "DH0010", "Patch method has multiple inner target attributes",
        "Method '{0}' has multiple inner target attributes; at most one is supported",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingTargetType = new(
        "DH0011", "Member selector has no declaring type",
        "Patch method '{0}' has a selector without a declaring type or a qualified member name",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NullInnerConstant = new(
        "DH0012", "Inner constant cannot be null", "Patch method '{0}' has a null inner constant",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor UnsupportedInnerAttribute = new(
        "DH0013", "Unsupported inner attribute type",
        "Patch method '{0}' uses an inner attribute that derives from neither InnerAttribute nor InnerConstantAttribute",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor DuplicateDiscoveryAttributes = new(
        "DH0014", "Duplicate patch discovery attributes",
        "Class '{0}' has multiple {1} attributes; use only one",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingMemberName = new(
        "DH0015", "Member selector requires a name",
        "Patch method '{0}' has a target or inner selector without a member name and does not select a constructor",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    private static void RegisterRegistryChecks(CompilationStartAnalysisContext start)
    {
        var patch = start.Compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
        var harmonyPatch = start.Compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatch");
        var category = start.Compilation.GetTypeByMetadataName("Disharmony.CategoryAttribute");
        var harmonyCategory = start.Compilation.GetTypeByMetadataName("HarmonyLib.HarmonyPatchCategory");
        var patchType = start.Compilation.GetTypeByMetadataName("Disharmony.PatchTypeAttribute");
        var innerBase = start.Compilation.GetTypeByMetadataName("Disharmony.InnerAttributeBase");
        var inner = start.Compilation.GetTypeByMetadataName("Disharmony.InnerAttribute");
        var constant = start.Compilation.GetTypeByMetadataName("Disharmony.InnerConstantAttribute");
        var target = start.Compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
        var targets = start.Compilation.GetTypeByMetadataName("Disharmony.TargetsAttribute");
        var memberType = start.Compilation.GetTypeByMetadataName("Disharmony.MemberType");
        var constructorKind = memberType?.GetMembers("Constructor").OfType<IFieldSymbol>()
            .FirstOrDefault()?.ConstantValue as int?;

        start.RegisterSymbolAction(ctx =>
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
                ctx.ReportDiagnostic(Diagnostic.Create(DuplicateDiscoveryAttributes, location, type.Name, "[Category]/[HarmonyPatchCategory]"));
        }, SymbolKind.NamedType);

        start.RegisterSymbolAction(ctx =>
        {
            var method = (IMethodSymbol)ctx.Symbol;
            if (method.IsImplicitlyDeclared || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor)
                return;
            var attributes = GetAttributes(method).Concat(GetAttributes(method.ContainingType)).ToArray();
            var patchTypes = attributes.Where(a => IsAttribute(a, patchType)).ToArray();
            var innerTargets = attributes.Where(a => IsAttribute(a, innerBase)).ToArray();
            var location = method.Locations.FirstOrDefault(l => l.IsInSource);
            if (location is null)
                return;
            if (patchTypes.Length > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(MultiplePatchTypes, location, method.Name));
            if (innerTargets.Length > 1)
                ctx.ReportDiagnostic(Diagnostic.Create(MultipleInnerTargets, location, method.Name));
            if (patchTypes.Length == 0)
                return;

            // A custom constructor or Harmony's type-name lookup can supply a type at runtime.
            // Warn only when every potential default is known not to supply one.
            bool mayHaveDefaultType = attributes.Any(a =>
                (IsAttribute(a, patch) && (!IsExactAttribute(a, patch) ||
                    Argument(a, "type") is not { IsNull: true })) ||
                (IsAttribute(a, harmonyPatch) && (!IsExactAttribute(a, harmonyPatch) ||
                    Argument(a, "typeName") is not null ||
                    Argument(a, "declaringType") is { IsNull: false })));
            foreach (var selector in attributes.Where(a => IsAttribute(a, target)))
            {
                if (!IsExactAttribute(selector, target) && !IsExactAttribute(selector, targets))
                    continue;
                if (!mayHaveDefaultType && HasNoTypeOrQualifiedName(selector))
                    ctx.ReportDiagnostic(Diagnostic.Create(MissingTargetType, SelectorLocation(selector, location), method.Name));
                CheckSelector(selector);
            }

            foreach (var selector in innerTargets)
            {
                if (!IsAttribute(selector, inner) && !IsAttribute(selector, constant))
                    ctx.ReportDiagnostic(Diagnostic.Create(UnsupportedInnerAttribute, SelectorLocation(selector, location), method.Name));
                else if (IsExactAttribute(selector, constant) && Argument(selector, "value") is { IsNull: true })
                    ctx.ReportDiagnostic(Diagnostic.Create(NullInnerConstant, SelectorLocation(selector, location), method.Name));
                else if (IsExactAttribute(selector, inner))
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
        }, SymbolKind.Method);
    }

    private static bool HasNoTypeOrQualifiedName(AttributeData selector)
    {
        if (Argument(selector, "type") is { IsNull: false })
            return false;
        var name = (Argument(selector, "methodName") ?? Argument(selector, "memberName"))?.Value as string;
        // Both Type:Member and Namespace.Type.Member are resolved using loaded assemblies at runtime.
        return name is null || (name.IndexOf(':') < 0 && name.IndexOf('.') < 0);
    }

    private static bool IsExactAttribute(AttributeData attribute, INamedTypeSymbol? expected) =>
        expected is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, expected);

    private static TypedConstant? Argument(AttributeData attribute, string name)
    {
        var parameters = attribute.AttributeConstructor?.Parameters;
        if (parameters is null)
            return null;
        for (int i = 0; i < parameters.Value.Length && i < attribute.ConstructorArguments.Length; i++)
        {
            if (parameters.Value[i].Name == name)
                return attribute.ConstructorArguments[i];
        }

        return null;
    }

    private static Location SelectorLocation(AttributeData attribute, Location fallback) =>
        attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? fallback;
}
