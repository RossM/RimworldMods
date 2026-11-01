using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Xylib.Analyzer;

internal sealed class RandChecks(Compilation compilation)
{
    private readonly IMethodSymbol? pushState = compilation.GetTypeByMetadataName("Verse.Rand")?
        .GetMembers("PushState").OfType<IMethodSymbol>()
        .FirstOrDefault(m => m.IsStatic && m.Arity == 0 && m.Parameters.Length == 1 &&
            m.Parameters[0].RefKind == RefKind.None && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);

    internal void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (pushState is not null && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod, pushState))
            context.ReportDiagnostic(Diagnostic.Create(XylibAnalyzer.UseRandBlock, invocation.Syntax.GetLocation()));
    }
}
