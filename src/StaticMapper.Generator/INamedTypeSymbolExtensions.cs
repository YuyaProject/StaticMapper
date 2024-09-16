using Microsoft.CodeAnalysis;

namespace StaticMapper.Generator;

public static class INamedTypeSymbolExtensions
{
	public static IEnumerable<INamedTypeSymbol> GetAllMembers(this INamespaceSymbol namespaceSymbol)
	{
		return namespaceSymbol.GetTypeMembers()
			.Concat(namespaceSymbol.GetNamespaceMembers().SelectMany(x => GetAllMembers(x)));
	}

	public static IEnumerable<ISymbol> GetAllMembers(this INamedTypeSymbol symbol)
	{
		if (symbol.BaseType != null)
		{
			return symbol.GetMembers()
				.Concat(symbol.BaseType.GetAllMembers());
		}
		return symbol.GetMembers();
	}
}