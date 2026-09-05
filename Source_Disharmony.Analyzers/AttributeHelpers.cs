using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Disharmony.Analyzers;

internal static class AttributeHelpers
{
    internal static int GetPatchOptions(IMethodSymbol method, INamedTypeSymbol? options)
    {
        var attribute = FindAttribute(method, options) ?? FindAttribute(method.ContainingType, options);
        return attribute is not null && Argument(attribute, "options")?.Value is int value ? value : 0;
    }

    internal static AttributeData? FindAttribute(ISymbol symbol, params INamedTypeSymbol?[] types) =>
        GetAttributes(symbol).FirstOrDefault(a => IsAttribute(a, types));

    // All supported attributes are inherited. Only target selectors and HarmonyPatch allow multiples.
    internal static IEnumerable<AttributeData> GetAttributes(ISymbol symbol)
    {
        var nearerTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        bool inherited = false;
        for (ISymbol? current = symbol;
             current is not null;
             current = current switch
             {
                 IMethodSymbol method => method.OverriddenMethod,
                 INamedTypeSymbol type => type.BaseType,
                 _ => null,
             })
        {
            var attributes = current.GetAttributes();
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass is not { } type)
                    continue;
                if (!inherited || !nearerTypes.Contains(type) || AllowsMultiple(type))
                    yield return attribute;
            }

            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass is { } type)
                    nearerTypes.Add(type);
            }

            inherited = true;
        }
    }

    private static bool AllowsMultiple(INamedTypeSymbol type) =>
        type.ToDisplayString() is "Disharmony.TargetAttribute" or "Disharmony.TargetsAttribute" or "HarmonyLib.HarmonyPatch";

    internal static bool IsAttribute(AttributeData attribute, params INamedTypeSymbol?[] types) =>
        types.Any(type => type is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, type));

    internal static TypedConstant? Argument(AttributeData attribute, string name)
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
}
