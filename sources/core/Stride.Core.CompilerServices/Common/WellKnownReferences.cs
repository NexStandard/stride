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
    public static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    internal static bool HasAttribute(this ISymbol symbol, INamedTypeSymbol attribute)
    {
        if (symbol.GetAttributes().Any(attr => attr.AttributeClass?.OriginalDefinition.Equals(attribute, SymbolEqualityComparer.Default) ?? false))
        {
            return true;
        }
        return false;
    }
}
