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
		_runId = RunId.GetNextRunId();
		// Register a factory that can create our custom syntax receiver
		context.RegisterForSyntaxNotifications(() => new StaticMapperSyntaxReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		// the generator infrastructure will create a receiver and populate it
		// we can retrieve the populated instance via the context
		if (!(context.SyntaxContextReceiver is StaticMapperSyntaxReceiver syntaxReceiver))
			return;

		var logfile = new CodeContainer(_runId, "Generated.g.cs");

		//StringBuilder sb = new("// Generated File - DO NOT EDIT" + Environment.NewLine + Environment.NewLine);

		foreach (var profileClass in syntaxReceiver.Classes.Where(x => x.Item2?.BaseType != null))
		{
			var semanticModel = context.Compilation.GetSemanticModel(profileClass.Item1.SyntaxTree);
			logfile.AddCodeLine("// " + profileClass.Item1.Identifier.ToFullString() + " - " + (profileClass.Item2?.BaseType?.Name ?? "Empty6"));
			var a = profileClass.Item1.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();
			var b = a.DescendantNodes().OfType<ConstructorInitializerSyntax>().First();
			var c = b.DescendantNodes().OfType<LiteralExpressionSyntax>().First();
			var mapperName = c.Token.ValueText;

			var mapConfigs = a.DescendantNodes().OfType<ExpressionStatementSyntax>()
				.Where(x => x.Expression.ToFullString().Trim().StartsWith("CreateMap<"))
				.Select(x =>
				{
					var d = x.DescendantNodes().OfType<GenericNameSyntax>().First().TypeArgumentList.Arguments;
					return (Source: semanticModel.GetTypeInfo(d[0]).Type ?? throw new InvalidOperationException(), Destination: semanticModel.GetTypeInfo(d[1]).Type ?? throw new InvalidOperationException());
				})
				.ToList();

			var mapperFile = new NamespacedCodeContainer(_runId, "M_" + mapperName + ".g.cs", profileClass.Item2.ContainingNamespace.ToDisplayString(), ["System", "System.Collections.Generic"]);

			//var sb2 = new StringBuilder($"// Generated File - DO NOT EDIT{Environment.NewLine}// {mapperName}.g.cs{Environment.NewLine}// {profileClass.Item2.ToDisplayString()}{Environment.NewLine}{Environment.NewLine}");
			//mapperFile.AddSingleCodeLine("using System;");
			//mapperFile.AddSingleCodeLine("using System.Collections.Generic;");
			//sb2.AppendLine();
			//mapperFile.AddSingleCodeLine("namespace " + profileClass.Item2.ContainingNamespace.ToDisplayString());
			//mapperFile.AddSingleCodeLine("{");
			mapperFile.AddSingleCodeLine("public partial interface I" + mapperName + " : StaticMapper.IMapper");
			mapperFile.AddSingleCodeLine("{");
			foreach (var (Source, Destination) in mapConfigs)
			{
				mapperFile.AddSingleCodeLine("\t// Source: " + Source.ToDisplayString() + ", Destination: " + Destination.ToDisplayString());
				mapperFile.AddSingleCodeLine($"\tvoid MapTo{Destination.Name}({Source.ToDisplayString()} source, {Destination.ToDisplayString()} destination);");
				mapperFile.AddSingleCodeLine($"\t{Destination.ToDisplayString()} MapTo{Destination.Name}({Source.ToDisplayString()} source);");
				mapperFile.AddSingleCodeLine($"\tIEnumerable<{Destination.ToDisplayString()}> MapTo{Destination.Name}(IEnumerable<{Source.ToDisplayString()}> source);");
				mapperFile.AddEmptyLine();
			}
			mapperFile.AddSingleCodeLine("}");
			mapperFile.AddEmptyLine();
			mapperFile.AddSingleCodeLine("internal partial class " + mapperName + " : I" + mapperName);
			mapperFile.AddSingleCodeLine("{");

			foreach (var (Source, Destination) in mapConfigs)
			{
				var sourceProperties = Source.GetMembers().OfType<IPropertySymbol>().ToList();
				var destinationProperties = Destination.GetMembers().OfType<IPropertySymbol>().ToList();
				var sameProperties = sourceProperties.Join(destinationProperties, x => x.Name, y => y.Name, (x, y) => (sp: x, dp: y)).ToList();

				mapperFile.AddSingleCodeLine($"\t// Source: {Source.ToDisplayString()}, Destination: {Destination.ToDisplayString()}");
				mapperFile.AddSingleCodeLine($"\tpublic void MapTo{Destination.Name}({Source.ToDisplayString()} source, {Destination.ToDisplayString()} destination)");
				mapperFile.AddSingleCodeLine("\t{");

				foreach (var (sp, dp) in sameProperties)
				{
					mapperFile.AddSingleCodeLine($"\t\tdestination.{dp.Name} = source.{sp.Name};");
				}

				mapperFile.AddSingleCodeLine("\t}");
				mapperFile.AddEmptyLine();

				mapperFile.AddSingleCodeLine($"\tpublic {Destination.ToDisplayString()} MapTo{Destination.Name}({Source.ToDisplayString()} source)");
				mapperFile.AddSingleCodeLine("\t{");
				mapperFile.AddSingleCodeLine("\t\treturn new " + Destination.ToDisplayString() + "()");
				mapperFile.AddSingleCodeLine("\t\t\t{");

				foreach (var (sp, dp) in sameProperties)
				{
					mapperFile.AddSingleCodeLine($"\t\t\t\t{dp.Name} = source.{sp.Name},");
				}
				mapperFile.AddSingleCodeLine("\t\t\t};");
				mapperFile.AddSingleCodeLine("\t}");
				mapperFile.AddEmptyLine();

				mapperFile.AddSingleCodeLine($"\tpublic IEnumerable<{Destination.ToDisplayString()}> MapTo{Destination.Name}(IEnumerable<{Source.ToDisplayString()}> source)");
				mapperFile.AddSingleCodeLine("\t{");
				mapperFile.AddSingleCodeLine("\t\tforeach(var item in source)");
				mapperFile.AddSingleCodeLine("\t\t{");
				mapperFile.AddSingleCodeLine("\t\t\tyield return MapTo" + Destination.Name + "(item);");
				mapperFile.AddSingleCodeLine("\t\t};");
				mapperFile.AddSingleCodeLine("\t}");
				mapperFile.AddEmptyLine();
			}

			mapperFile.AddSingleCodeLine("}");

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
				//&& context.SemanticModel.GetTypeInfo(cls).Type?.BaseType.ToDisplayString() == "StaticMapper.Profile"
				)
			{
				Classes.Add((cls, nts));
			}
		}
	}
}