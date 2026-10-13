using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Xylib.Analyzer;

internal sealed class PatchChecks
{
    private readonly INamedTypeSymbol? patch;
    private readonly INamedTypeSymbol? prefix;
    private readonly INamedTypeSymbol? postfix;
    private readonly INamedTypeSymbol? target;
    private readonly INamedTypeSymbol? feature;

    internal PatchChecks(Compilation compilation)
    {
        patch = compilation.GetTypeByMetadataName("Disharmony.PatchAttribute");
        prefix = compilation.GetTypeByMetadataName("Disharmony.PrefixAttribute");
        postfix = compilation.GetTypeByMetadataName("Disharmony.PostfixAttribute");
        // TargetsAttribute derives from TargetAttribute.
        target = compilation.GetTypeByMetadataName("Disharmony.TargetAttribute");
        feature = compilation.GetTypeByMetadataName("Xylib.FeatureAttribute");
    }

    internal void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind == TypeKind.Class && !type.IsStatic && SymbolHelpers.HasAttribute(type, patch) &&
            SymbolHelpers.SourceLocation(type) is { } location)
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.StaticPatchClass, location, type.Name));
    }

    internal void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind != MethodKind.Ordinary || !SymbolHelpers.HasAttribute(method.ContainingType, patch) ||
            SymbolHelpers.SourceLocation(method) is not { } location)
            return;

        bool isPrefix = SymbolHelpers.HasAttribute(method, prefix);
        bool isPostfix = SymbolHelpers.HasAttribute(method, postfix);
        bool isPatch = isPrefix || isPostfix || SymbolHelpers.HasAttribute(method, target);
        bool hasFeature = SymbolHelpers.HasAttribute(method, feature);

        if (feature is not null && isPatch != hasFeature)
            context.ReportDiagnostic(Diagnostic.Create(
                isPatch ? XylibAnalyzer.MissingFeature : XylibAnalyzer.FeatureWithoutPatch, location, method.Name));

        if (isPrefix && method.Name != "Prefix" && !method.Name.EndsWith("_Prefix", StringComparison.Ordinal))
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.PrefixName, location, method.Name));
        if (isPostfix && method.Name != "Postfix" && !method.Name.EndsWith("_Postfix", StringComparison.Ordinal))
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.PostfixName, location, method.Name));
    }
}
