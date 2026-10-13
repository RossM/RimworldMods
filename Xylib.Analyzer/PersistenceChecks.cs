using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Xylib.Analyzer;

internal sealed class PersistenceChecks
{
    private readonly Compilation compilation;
    private readonly IMethodSymbol? exposeData;
    private readonly INamedTypeSymbol? thingComp;
    private readonly INamedTypeSymbol? unsaved;

    internal PersistenceChecks(Compilation compilation)
    {
        this.compilation = compilation;
        exposeData = compilation.GetTypeByMetadataName("Verse.IExposable")?.GetMembers("ExposeData").OfType<IMethodSymbol>().FirstOrDefault();
        thingComp = compilation.GetTypeByMetadataName("Verse.ThingComp");
        unsaved = compilation.GetTypeByMetadataName("Verse.UnsavedAttribute");
    }

    internal void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || SaveMethodName(type) is not { } methodName)
            return;

        // Each type is responsible for its own fields; an inherited method cannot expose newly declared fields.
        var method = DeclaredSaveMethod(type, methodName);
        var body = method is null ? null : MethodBody.Read(method, compilation, context.CancellationToken);
        foreach (var field in FieldsToSave(type))
        {
            if (body?.References(field) != true && SymbolHelpers.SourceLocation(field) is { } location)
                context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.UnsavedField, location, field.Name, methodName));
        }

        if (method is null || body is null || SymbolHelpers.BaseMethod(method, compilation) is not { IsAbstract: false } baseMethod)
            return;

        bool hasBaseData = SymbolHelpers.BaseTypesAndSelf(baseMethod.ContainingType).Any(t => FieldsToSave(t).Any());
        if (hasBaseData && !body.Calls(baseMethod) && SymbolHelpers.SourceLocation(method) is { } methodLocation)
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.MissingExposeDataBaseCall, methodLocation,
                method.ToDisplayString(), baseMethod.ToDisplayString()));
    }

    private IEnumerable<IFieldSymbol> FieldsToSave(INamedTypeSymbol type) => type.GetMembers().OfType<IFieldSymbol>()
        .Where(f => !f.IsImplicitlyDeclared && !f.IsStatic && !f.IsConst && !f.IsReadOnly && !SymbolHelpers.HasAttribute(f, unsaved));

    private string? SaveMethodName(INamedTypeSymbol type)
    {
        if (exposeData is not null && type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, exposeData.ContainingType)))
            return "ExposeData";
        if (SymbolHelpers.DerivesFrom(type, thingComp))
            return "PostExposeData";
        return SymbolHelpers.BaseTypesAndSelf(type).Any(t => t.Name.EndsWith("Comp", StringComparison.Ordinal))
            ? "CompExposeData"
            : null;
    }

    private IMethodSymbol? DeclaredSaveMethod(INamedTypeSymbol type, string name)
    {
        if (name == "ExposeData" && exposeData is not null)
        {
            if (type.FindImplementationForInterfaceMember(exposeData) is not IMethodSymbol implementation)
                return null;
            // A same-named hidden method does not implement the interface; a declared override does.
            return type.GetMembers().OfType<IMethodSymbol>()
                .FirstOrDefault(m => SymbolHelpers.OverridesOrEquals(m, implementation));
        }

        return type.GetMembers(name).OfType<IMethodSymbol>()
            .FirstOrDefault(m => !m.IsStatic && m.Arity == 0 && m.Parameters.Length == 0 && m.ReturnsVoid);
    }
}
