using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Xylib.Analyzer;

internal sealed class ApiUsageChecks(Compilation compilation)
{
    private readonly IMethodSymbol? pushState = compilation.GetTypeByMetadataName("Verse.Rand")?
        .GetMembers("PushState").OfType<IMethodSymbol>()
        .FirstOrDefault(m => m.IsStatic && m.Arity == 0 && m.Parameters.Length == 1 &&
            m.Parameters[0].RefKind == RefKind.None && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32);

    private readonly INamedTypeSymbol? geneDef = compilation.GetTypeByMetadataName("Verse.GeneDef");
    private readonly INamedTypeSymbol? geneWithComps = compilation.GetTypeByMetadataName("Xylib.DefModExtension_GeneWithComps");
    private readonly IMethodSymbol? getModExtension = compilation.GetTypeByMetadataName("Verse.Def")?
        .GetMembers("GetModExtension").OfType<IMethodSymbol>()
        .FirstOrDefault(m => !m.IsStatic && m.Arity == 1 && m.Parameters.Length == 0);

    private readonly IFieldSymbol? pawnGenes = compilation.GetTypeByMetadataName("Verse.Pawn")?
        .GetMembers("genes").OfType<IFieldSymbol>().FirstOrDefault(f => !f.IsStatic);
    private readonly IMethodSymbol? hasActiveGene = compilation.GetTypeByMetadataName("RimWorld.Pawn_GeneTracker")?
        .GetMembers("HasActiveGene").OfType<IMethodSymbol>()
        .FirstOrDefault(m => !m.IsStatic && m.Arity == 0 && m.Parameters.Length == 1 &&
            m.Parameters[0].RefKind == RefKind.None &&
            SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, compilation.GetTypeByMetadataName("Verse.GeneDef")));

    private readonly IMethodSymbol? getFirstGeneOfType = compilation.GetTypeByMetadataName("RimWorld.Pawn_GeneTracker")?
        .GetMembers("GetFirstGeneOfType").OfType<IMethodSymbol>()
        .FirstOrDefault(m => !m.IsStatic && m.Arity == 1 && m.Parameters.Length == 0);

    internal void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        DiagnosticDescriptor? rule = null;
        if (pushState is not null && SymbolEqualityComparer.Default.Equals(method, pushState))
            rule = XylibAnalyzer.UseRandBlock;
        else if (getModExtension is not null && geneWithComps is not null &&
                 SymbolEqualityComparer.Default.Equals(method.OriginalDefinition, getModExtension) &&
                 SymbolEqualityComparer.Default.Equals(method.TypeArguments[0], geneWithComps) &&
                 invocation.Instance?.Type is INamedTypeSymbol receiver && SymbolHelpers.DerivesFrom(receiver, geneDef))
            rule = XylibAnalyzer.UseGeneWithCompsExtension;
        else if (IsPawnGenes(invocation.Instance))
        {
            if (hasActiveGene is not null && SymbolEqualityComparer.Default.Equals(method, hasActiveGene))
                rule = XylibAnalyzer.UsePawnHasActiveGene;
            else if (getFirstGeneOfType is not null && SymbolEqualityComparer.Default.Equals(method.OriginalDefinition, getFirstGeneOfType))
                rule = XylibAnalyzer.UsePawnFirstActiveGeneOfType;
        }

        if (rule is not null)
            context.ReportDiagnostic(Diagnostic.Create(rule, invocation.Syntax.GetLocation()));
    }

    private bool IsPawnGenes(IOperation? receiver)
    {
        while (true)
        {
            switch (receiver)
            {
                case IConditionalAccessInstanceOperation:
                    // The placeholder represents the receiver of the nearest enclosing conditional access.
                    var parent = receiver.Parent;
                    while (parent is not null && parent is not IConditionalAccessOperation)
                        parent = parent.Parent;
                    receiver = (parent as IConditionalAccessOperation)?.Operation;
                    break;
                case IConditionalAccessOperation conditional:
                    receiver = conditional.WhenNotNull;
                    break;
                case IParenthesizedOperation parenthesized:
                    receiver = parenthesized.Operand;
                    break;
                case IConversionOperation { IsImplicit: true } conversion:
                    receiver = conversion.Operand;
                    break;
                default:
                    return pawnGenes is not null && receiver is IFieldReferenceOperation field &&
                           SymbolEqualityComparer.Default.Equals(field.Field, pawnGenes);
            }
        }
    }
}
