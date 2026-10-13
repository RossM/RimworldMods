using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Xylib.Analyzer;

internal static class SymbolHelpers
{
    internal static IEnumerable<INamedTypeSymbol> BaseTypesAndSelf(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
            yield return current;
    }

    internal static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol? baseType) =>
        baseType is not null && BaseTypesAndSelf(type).Any(t => SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, baseType.OriginalDefinition));

    internal static bool OverridesOrEquals(IMethodSymbol method, IMethodSymbol implementation)
    {
        for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
            if (SymbolEqualityComparer.Default.Equals(current, implementation))
                return true;
        return false;
    }

    internal static IMethodSymbol? BaseMethod(IMethodSymbol method, Compilation compilation)
    {
        if (method.OverriddenMethod is { } overridden)
            return overridden;
        if (method.ContainingType.BaseType is not { } baseType)
            return null;

        return BaseTypesAndSelf(baseType).SelectMany(t => t.GetMembers(method.Name)).OfType<IMethodSymbol>()
            .FirstOrDefault(m => !m.IsStatic && m.Arity == method.Arity && m.Parameters.Length == method.Parameters.Length &&
                compilation.IsSymbolAccessibleWithin(m, method.ContainingType) &&
                m.Parameters.Zip(method.Parameters, (a, b) => a.RefKind == b.RefKind && SymbolEqualityComparer.Default.Equals(a.Type, b.Type)).All(equal => equal));
    }

    internal static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType, bool inherit = true)
    {
        if (attributeType is null)
            return false;

        for (ISymbol? current = symbol; current is not null; current = current switch
             {
                 INamedTypeSymbol type => type.BaseType,
                 IMethodSymbol method => method.OverriddenMethod,
                 _ => null,
             })
        {
            foreach (var attribute in current.GetAttributes())
            {
                if (attribute.AttributeClass is { } type && DerivesFrom(type, attributeType) &&
                    (SymbolEqualityComparer.Default.Equals(current, symbol) || IsInherited(type)))
                    return true;
            }

            if (!inherit)
                break;
        }

        return false;
    }

    private static bool IsInherited(INamedTypeSymbol attributeType)
    {
        foreach (var type in BaseTypesAndSelf(attributeType))
        {
            var usage = type.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.AttributeUsageAttribute");
            if (usage is not null)
                return !usage.NamedArguments.Any(a => a.Key == "Inherited" && a.Value.Value is false);
        }

        return true;
    }

    internal static Location? SourceLocation(ISymbol symbol) => symbol.Locations.FirstOrDefault(l => l.IsInSource);
}
