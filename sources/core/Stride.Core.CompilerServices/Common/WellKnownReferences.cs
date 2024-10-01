namespace Stride.Core.CompilerServices.Common;
internal static class WellKnownReferences
{
    internal static INamedTypeSymbol? DataMemberAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberAttribute");
    }

    internal static INamedTypeSymbol? IDictionary_generic(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName);
    }

    internal static INamedTypeSymbol? DataMemberIgnoreAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberIgnoreAttribute");
    }

    internal static INamedTypeSymbol? DataMemberMode(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataMemberMode");
    }

    internal static INamedTypeSymbol? DataMemberUpdatableAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Updater.DataMemberUpdatableAttribute");
    }

    internal static INamedTypeSymbol? DataContractAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName("Stride.Core.DataContractAttribute");
    }

    internal static INamedTypeSymbol? ModuleInitializerAttribute(Compilation compilation)
    {
        return compilation.GetTypeByMetadataName(ModuleInitializerAttributeName);
    }
    internal const string ModuleInitializerAttributeName = "Stride.Core.ModuleInitializerAttribute";
    /// <summary>
    /// Checks the attributes applied to a symbol and determines if any of them
    /// match the given attribute type.
    /// </summary>
    /// <param name="symbol">The <see cref="ISymbol"/> to check for the attribute.</param>
    /// <param name="attribute">The <see cref="INamedTypeSymbol"/> representing the attribute type to look for.</param>
    /// <returns>
    /// Returns <c>true</c> if the symbol has the specified attribute; otherwise, <c>false</c>.
    /// </returns>
    internal static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
}
