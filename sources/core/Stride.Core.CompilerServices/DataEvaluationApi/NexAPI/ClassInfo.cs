using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Xml.Linq;

namespace Stride.Core.CompilerServices.DataEvaluationApi.NexAPI;
internal class ClassInfo : IEquatable<ClassInfo>
{
    private const string GeneratorPrefix = "StrideSourceGenerated_";

    public static ClassInfo CreateFrom(ITypeSymbol type, ImmutableList<SymbolInfo> members)
    {
        var isGeneric = false;
        var generatorName = GeneratorPrefix + GetFullNamespace(type, '_') + type.Name;
        var genericTypeName = "";
        if (type is INamedTypeSymbol namedType)
        {
            if (namedType.TypeArguments != null)
            {

                var genericcount = namedType.TypeArguments.Count();
                if (genericcount > 0)
                {
                    isGeneric = true;
                    genericTypeName = type.Name + "<" + new string(',', genericcount - 1) + ">";
                }
            }
        }
        var namespaceName = GetFullNamespace(type, '.');
        return new()
        {
            Name = type.Name,
            IsGeneric = isGeneric,
            GenericTypeName = genericTypeName,
            NameSpace = namespaceName,
            AllInterfaces = type.AllInterfaces.Select(t => t.Name).ToList(),
            AllAbstracts = FindAbstractClasses(type),
            Accessor = type.DeclaredAccessibility.ToString().ToLower(),
            GeneratorName = generatorName,
            MemberSymbols = members
        };
    }
    static string GetFullNamespace(ITypeSymbol typeSymbol, char separator)
    {
        var namespaceSymbol = typeSymbol.ContainingNamespace;
        var fullNamespace = "";

        while (namespaceSymbol != null && !string.IsNullOrEmpty(namespaceSymbol.Name))
        {
            fullNamespace = namespaceSymbol.Name + separator + fullNamespace;
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return fullNamespace.TrimEnd(separator);
    }


    private ClassInfo() { }
    public bool IsGeneric { get; private set; }
    public string GenericTypeName { get; private set; }
    public string Name { get; set; }
    public string NameSpace { get; set; }
    public string GeneratorName { get; set; }
    public string Accessor { get; set; }
    internal IReadOnlyList<string> AllInterfaces { get; set; }
    internal IReadOnlyList<string> AllAbstracts { get; set; }
    public ImmutableList<SymbolInfo> MemberSymbols { get; internal set; }

    public bool Equals(ClassInfo other)
    {
        return Name == other.Name && NameSpace == other.NameSpace && GeneratorName == other.GeneratorName;
    }
    private static IReadOnlyList<string> FindAbstractClasses(ITypeSymbol typeSymbol)
    {
        var result = new List<string>();
        var baseType = typeSymbol.BaseType;
        while (baseType != null)
        {
            if (baseType.IsAbstract)
            {
                result.Add(baseType.Name);
            }
            baseType = baseType.BaseType;
        }
        return result;
    }
}