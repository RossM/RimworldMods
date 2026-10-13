using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Xylib.Analyzer;

internal sealed class ConfigErrorsChecks(Compilation compilation)
{
    internal void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.Name != "ConfigErrors" || method.IsStatic || method.MethodKind != MethodKind.Ordinary ||
            SymbolHelpers.BaseMethod(method, compilation) is not { IsAbstract: false } baseMethod)
            return;

        var body = MethodBody.Read(method, compilation, context.CancellationToken);
        if (body is not null && !body.Calls(baseMethod) && SymbolHelpers.SourceLocation(method) is { } location)
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.MissingConfigErrorsBaseCall, location,
                method.ToDisplayString(), baseMethod.ToDisplayString()));
    }
}
