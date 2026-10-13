using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Xylib.Analyzer;

// Records evidence about this instance, without attempting to prove persistence or control flow.
internal sealed class MethodBody : OperationWalker
{
    private readonly HashSet<IFieldSymbol> fields = new(SymbolEqualityComparer.Default);
    private readonly HashSet<IMethodSymbol> calls = new(SymbolEqualityComparer.Default);

    private MethodBody(IOperation operation) => Visit(operation);

    internal bool References(IFieldSymbol field) => fields.Contains(field);
    internal bool Calls(IMethodSymbol method) => calls.Contains(method);

    internal static MethodBody? Read(IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken)
    {
        method = method.PartialImplementationPart ?? method;
        foreach (var reference in method.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not MethodDeclarationSyntax declaration)
                continue;
            var syntax = (SyntaxNode?)declaration.Body ?? declaration.ExpressionBody?.Expression;
            if (syntax is not null && compilation.GetSemanticModel(syntax.SyntaxTree).GetOperation(syntax, cancellationToken) is { } operation)
                return new MethodBody(operation);
        }

        return null;
    }

    public override void VisitFieldReference(IFieldReferenceOperation operation)
    {
        if (operation.Instance is IInstanceReferenceOperation)
            fields.Add(operation.Field);
        base.VisitFieldReference(operation);
    }

    public override void VisitInvocation(IInvocationOperation operation)
    {
        if (operation.Instance is IInstanceReferenceOperation)
            calls.Add(operation.TargetMethod);
        base.VisitInvocation(operation);
    }

    // nameof produces neither a field access nor a method call.
    public override void VisitNameOf(INameOfOperation operation) { }
}
