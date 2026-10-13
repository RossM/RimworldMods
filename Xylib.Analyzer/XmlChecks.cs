using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Xylib.Analyzer;

internal sealed class XmlChecks
{
    private readonly INamedTypeSymbol? usedFromXml;
    private readonly INamedTypeSymbol[] xmlTypes;

    internal XmlChecks(Compilation compilation)
    {
        usedFromXml = compilation.GetTypeByMetadataName("Xylib.UsedFromXmlAttribute");
        xmlTypes = new[]
        {
            "Verse.AbilityCompProperties", "Verse.ColorGenerator", "Verse.CompProperties", "Verse.Def",
            "Verse.DefModExtension", "Verse.HediffCompProperties", "Verse.HediffGiver", "RimWorld.IngestionOutcomeDoer",
            "Verse.PatchOperation", "Verse.PawnRenderNodeProperties", "RimWorld.ScenPart",
        }.Select(compilation.GetTypeByMetadataName).OfType<INamedTypeSymbol>().ToArray();
    }

    internal void AnalyzeType(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (usedFromXml is null || type.TypeKind != TypeKind.Class || type.IsAbstract ||
            SymbolHelpers.HasAttribute(type, usedFromXml, inherit: false) || SymbolHelpers.SourceLocation(type) is not { } location)
            return;

        // Walk the actual ancestry, including referenced assemblies, rather than enumerating loaded game types.
        var xmlBase = SymbolHelpers.BaseTypesAndSelf(type).FirstOrDefault(t =>
            xmlTypes.Any(known => SymbolEqualityComparer.Default.Equals(t, known)) ||
            (t.IsAbstract && SymbolHelpers.HasAttribute(t, usedFromXml, inherit: false)));
        if (xmlBase is not null)
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.MissingUsedFromXml, location, type.Name, xmlBase.Name));
    }
}
