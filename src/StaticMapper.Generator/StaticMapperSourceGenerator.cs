using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StaticMapper.Generator;

[Generator]
public class StaticMapperSourceGenerator : ISourceGenerator
{
	private static int _runId;

	public void Initialize(GeneratorInitializationContext context)
	{
#pragma warning disable S2696 // Instance members should not write to "static" fields
		_runId = RunId.GetNextRunId();
#pragma warning restore S2696 // Instance members should not write to "static" fields
		// Register a factory that can create our custom syntax receiver
		context.RegisterForSyntaxNotifications(() => new StaticMapperSyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		// the generator infrastructure will create a receiver and populate it
		// we can retrieve the populated instance via the context
		if (context.SyntaxContextReceiver is not StaticMapperSyntaxReceiver syntaxReceiver)
			return;

		var logfile = new CodeContainer(_runId, "Generated.g.cs");

		foreach (var profileClass in syntaxReceiver.Classes.Where(x => x.Item2?.BaseType != null))
		{
			var semanticModel = context.Compilation.GetSemanticModel(profileClass.Item1.SyntaxTree);
			logfile.AddCodeLine($"// {profileClass.Item1.Identifier.ToFullString()} - {profileClass.Item2?.BaseType?.Name ?? "Empty6"}");
			var a = profileClass.Item1.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
			var b = a.DescendantNodes().OfType<ConstructorInitializerSyntax>().First();
			var c = b.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
			var mapperName = c.Token.ValueText;


			var mapConfigs = new List<(ExpressionStatementSyntax StatementSyntax, ITypeSymbol Source, ITypeSymbol Destination)>();

			foreach (var x in a.DescendantNodes().OfType<ExpressionStatementSyntax>()
				.Where(x => x.Expression.ToFullString().Trim().StartsWith("CreateMap<")))
			{
				var d = x.Expression.DescendantNodes().OfType<GenericNameSyntax>().First().TypeArgumentList.Arguments;
				var source = semanticModel.GetTypeInfo(d[0]).Type ?? throw new InvalidOperationException();
				var destination = semanticModel.GetTypeInfo(d[1]).Type ?? throw new InvalidOperationException();
				var withReverse = x.Expression.DescendantNodes().OfType<IdentifierNameSyntax>().Any(x => x.Identifier.Text == "WithReverse");
				mapConfigs.Add((StatementSyntax: x, Source: source, Destination: destination)); 
				if (withReverse) mapConfigs.Add((StatementSyntax: x, Source: destination, Destination: source));
			}

			var mapperFile = new NamespacedCodeContainer(_runId, $"M_{mapperName}.g.cs", profileClass.Item2!.ContainingNamespace.ToDisplayString(), ["System", "System.Collections.Generic"]);

			mapperFile.AddMultilinedCodeText($@"
#nullable enable
public partial interface I{mapperName} : StaticMapper.IMapper<{profileClass.Item2.ToDisplayString()}>
{{");
			foreach (var (StatementSyntax, Source, Destination) in mapConfigs)
			{
				mapperFile.AddMultilinedCodeText($@"	// Source: {Source.ToDisplayString()}, Destination: {Destination.ToDisplayString()}
	void Map({Source.ToDisplayString()} source, {Destination.ToDisplayString()} destination);
	{Destination.ToDisplayString()} MapTo{Destination.Name}({Source.ToDisplayString()} source);
	IEnumerable<{Destination.ToDisplayString()}> MapTo{Destination.Name}(IEnumerable<{Source.ToDisplayString()}> source);
");
			}

			static string mapFunc1((ExpressionStatementSyntax StatementSyntax, ITypeSymbol Source, ITypeSymbol Destination) x, int index) => $$"""

		// Source: {{x.Source.ToDisplayString()}}, Destination: {{x.Destination.ToDisplayString()}}
		if(typeof(TDestination) == typeof({{x.Destination.ToDisplayString()}}) && source is {{x.Source.ToDisplayString()}} s{{index}})
			return MapTo{{x.Destination.Name}}(s{{index}}) as TDestination ?? throw new InvalidCastException($"Unable to cast mapped result to {typeof(TDestination).FullName}");
		if(typeof(TDestination) == typeof(List<{{x.Destination.ToDisplayString()}}>) && source is IEnumerable<{{x.Source.ToDisplayString()}}> s{{index}}a)
			return MapTo{{x.Destination.Name}}(s{{index}}a).ToList() as TDestination ?? throw new InvalidCastException($"Unable to cast mapped result to {typeof(TDestination).FullName}");
		if(typeof(TDestination) == typeof({{x.Destination.ToDisplayString()}}[]) && source is IEnumerable<{{x.Source.ToDisplayString()}}> s{{index}}b)
			return MapTo{{x.Destination.Name}}(s{{index}}b).ToArray() as TDestination ?? throw new InvalidCastException($"Unable to cast mapped result to {typeof(TDestination).FullName}");
		if(typeof(TDestination) == typeof(IEnumerable<{{x.Destination.ToDisplayString()}}>) && source is IEnumerable<{{x.Source.ToDisplayString()}}> s{{index}}c)
			return MapTo{{x.Destination.Name}}(s{{index}}c) as TDestination ?? throw new InvalidCastException($"Unable to cast mapped result to {typeof(TDestination).FullName}");
""";

			static string mapFunc2((ExpressionStatementSyntax StatementSyntax, ITypeSymbol Source, ITypeSymbol Destination) x, int index) => $@"		if(source is {x.Source.ToDisplayString()} s{index} && destination is {x.Destination.ToDisplayString()} d{index})
			Map(s{index}, d{index});";

			static string mapFunc3((ExpressionStatementSyntax StatementSyntax, ITypeSymbol Source, ITypeSymbol Destination) x, int index) => $@"		if(source is {x.Source.ToDisplayString()} s{index} && destination is {x.Destination.ToDisplayString()} d{index})
			Map(s{index}, d{index});";

			mapperFile.AddMultilinedCodeText($$"""
}

internal partial class {{mapperName}} : I{{mapperName}}
{
	private {{profileClass.Item2.ToDisplayString()}}? _profile = null;

	public {{profileClass.Item2.ToDisplayString()}} Profile => _profile ??= new {{profileClass.Item2.ToDisplayString()}}();

	public TDestination Map<TDestination>(object source)
		where TDestination : class
	{
		ArgumentNullException.ThrowIfNull(source);
{{string.Join(NamespacedCodeContainer.NewLine, mapConfigs.Select(mapFunc1))}}

		throw new NotImplementedException($"Mapping from {source.GetType().Name} to {typeof(TDestination).Name} is not implemented.");
	}

	/*
	public void Map<TSource, TDestination>(TSource source, TDestination destination)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);

{{string.Join(NamespacedCodeContainer.NewLine, mapConfigs.Select(mapFunc2))}}

		throw new NotImplementedException($"Mapping from {source.GetType().Name} to {typeof(TDestination).Name} is not implemented.");
	}
	*/

	public void Map(object source, object destination)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);

{{string.Join(NamespacedCodeContainer.NewLine, mapConfigs.Select(mapFunc3))}}

		throw new NotImplementedException($"Mapping from {source.GetType().Name} to {destination.GetType().Name} is not implemented.");
	}

""");

			foreach (var (StatementSyntax, Source, Destination) in mapConfigs)
			{
				var sourceProperties = Source.GetMembers().OfType<IPropertySymbol>().ToList();
				var destinationProperties = Destination.GetMembers().OfType<IPropertySymbol>().ToList();
				var sameProperties = sourceProperties.Join(destinationProperties, x => x.Name, y => y.Name, (x, y) => (sp: x, dp: y)).ToList();

				mapperFile.AddMultilinedCodeText($@"	// Source: {Source.ToDisplayString()}, Destination: {Destination.ToDisplayString()}
	public void Map({Source.ToDisplayString()} source, {Destination.ToDisplayString()} destination)
	{{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);

{string.Join(NamespacedCodeContainer.NewLine, sameProperties.Select(x => $"\t\tdestination.{x.dp.Name} = source.{x.sp.Name};"))}
	}}

	public {Destination.ToDisplayString()} MapTo{Destination.Name}({Source.ToDisplayString()} source){{
		var destination = new {Destination.ToDisplayString()}();
		Map(source, destination);
		return destination;
	}}

	public IEnumerable<{Destination.ToDisplayString()}> MapTo{Destination.Name}(IEnumerable<{Source.ToDisplayString()}> source)
	{{
		foreach(var item in source)
			yield return MapTo{Destination.Name}(item);
	}}
");
			}

			mapperFile.AddSingleCodeLine("}");
			mapperFile.AddSingleCodeLine("#nullable disable");

			logfile.AddCodeLine("//    " + mapperName);
			logfile.AddCodeLine("");

			mapperFile.WriteCodeToFile(context);
		}

		logfile.WriteCodeToFile(context);
	}

	private sealed class StaticMapperSyntaxReceiver : ISyntaxContextReceiver
	{
		public List<(ClassDeclarationSyntax, INamedTypeSymbol?)> Classes { get; private set; } = new();

		public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
		{
			// find all valid mustache attributes
			if (context.Node is ClassDeclarationSyntax cls
				&& cls.BaseList?.Types.Count > 0
				&& context.SemanticModel.GetDeclaredSymbol(cls) is INamedTypeSymbol nts
				&& nts.BaseType?.ToDisplayString() == "StaticMapper.Profile"
				)
			{
				Classes.Add((cls, nts));
			}
		}
	}
}