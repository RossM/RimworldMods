using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Xylib.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XylibAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor StaticPatchClass = new(
        "XYLIB0001", "Patch class should be static", "Patch class '{0}' should be static",
        "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingFeature = new(
        "XYLIB0002", "Patch method should document its feature", "Patch method '{0}' is missing a [Feature] attribute",
        "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor FeatureWithoutPatch = new(
        "XYLIB0003", "Feature attribute belongs on a patch method", "Method '{0}' has [Feature] but no patch attribute",
        "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PrefixName = new(
        "XYLIB0004", "Prefix should follow the naming convention", "Prefix '{0}' should be named 'Prefix' or end with '_Prefix'",
        "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PostfixName = new(
        "XYLIB0005", "Postfix should follow the naming convention", "Postfix '{0}' should be named 'Postfix' or end with '_Postfix'",
        "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingUsedFromXml = new(
        "XYLIB0006", "XML type should be annotated", "Class '{0}' derives from XML type '{1}' but does not declare [UsedFromXml]",
        "Style", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingConfigErrorsBaseCall = new(
        "XYLIB0007", "ConfigErrors should call its base implementation", "Method '{0}' is missing a call to '{1}'",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsavedField = new(
        "XYLIB0008", "Instance field appears unsaved",
        "Field '{0}' appears not to be saved in '{1}'; save it, mark it [Unsaved], or make it readonly",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingExposeDataBaseCall = new(
        "XYLIB0009", "Save method should preserve base data", "Method '{0}' is missing a call to '{1}', whose type has fields to save",
        "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [
        StaticPatchClass, MissingFeature, FeatureWithoutPatch, PrefixName, PostfixName, MissingUsedFromXml,
        MissingConfigErrorsBaseCall, UnsavedField, MissingExposeDataBaseCall,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var patches = new PatchChecks(start.Compilation);
            var xml = new XmlChecks(start.Compilation);
            var persistence = new PersistenceChecks(start.Compilation);
            var configErrors = new ConfigErrorsChecks(start.Compilation);
            start.RegisterSymbolAction(patches.AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(patches.AnalyzeMethod, SymbolKind.Method);
            start.RegisterSymbolAction(xml.AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(persistence.AnalyzeType, SymbolKind.NamedType);
            start.RegisterSymbolAction(configErrors.AnalyzeMethod, SymbolKind.Method);
        });
    }
}
