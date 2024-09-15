using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StaticMapper.Generator
{
    [Generator]
    public class StaticMapperSourceGenerator : ISourceGenerator
    {

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new StaticMapperSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // the generator infrastructure will create a receiver and populate it
            // we can retrieve the populated instance via the context
            //StaticMapperSyntaxReceiver syntaxReceiver = context.SyntaxContextReceiver as StaticMapperSyntaxReceiver ?? throw new InvalidOperationException();
            if (!(context.SyntaxContextReceiver is StaticMapperSyntaxReceiver syntaxReceiver))
                return;

            //var profileClasses = syntaxReceiver.Classes.Select(x => (classSyntax: x, semanticModel: context.Compilation.GetSemanticModel(x.SyntaxTree)))
            //    .ToList();

            StringBuilder sb = new StringBuilder("// Generated File - DO NOT EDIT" + Environment.NewLine + Environment.NewLine);

            foreach (var profileClass in syntaxReceiver.Classes.Where(x => x.Item2?.BaseType != null))
            {
                var semanticModel = context.Compilation.GetSemanticModel(profileClass.Item1.SyntaxTree);
                sb.AppendLine("// " + profileClass.Item1.Identifier.ToFullString() + " - " + (profileClass.Item2?.BaseType?.Name ?? "Empty6"));
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
                    .ToList(); //TODO: Make it generic and use it in other places>


                var sb2 = new StringBuilder($"// Generated File - DO NOT EDIT{Environment.NewLine}// {mapperName}.g.cs{Environment.NewLine}// {profileClass.Item2.ToDisplayString()}{Environment.NewLine}{Environment.NewLine}");
                sb2.AppendLine("using System;");
                sb2.AppendLine("using System.Collections.Generic;");
                sb2.AppendLine();
                sb2.AppendLine("namespace " + profileClass.Item2.ContainingNamespace.ToDisplayString());
                sb2.AppendLine("{");
                sb2.AppendLine("\tpublic partial interface I" + mapperName + " : StaticMapper.IMapper");
                sb2.AppendLine("\t{");
                foreach (var (Source, Destination) in mapConfigs)
                {
                    sb2.AppendLine("\t\t// Source: " + Source.ToDisplayString() + ", Destination: " + Destination.ToDisplayString());
                    sb2.AppendLine($"\t\tvoid MapTo{Destination.Name}({Source.ToDisplayString()} source, {Destination.ToDisplayString()} destination);");
                    sb2.AppendLine($"\t\t{Destination.ToDisplayString()} MapTo{Destination.Name}({Source.ToDisplayString()} source);");
                    sb2.AppendLine($"\t\tIEnumerable<{Destination.ToDisplayString()}> MapTo{Destination.Name}(IEnumerable<{Source.ToDisplayString()}> source);");
                    sb2.AppendLine();
                }
                sb2.AppendLine("\t}");
                sb2.AppendLine();
                sb2.AppendLine("\tinternal partial class " + mapperName + " : I" + mapperName);
                sb2.AppendLine("\t{");

                foreach (var (Source, Destination) in mapConfigs)
                {
                    var sourceProperties = Source.GetMembers().OfType<IPropertySymbol>().ToList();
                    var destinationProperties = Destination.GetMembers().OfType<IPropertySymbol>().ToList();
                    var sameProperties = sourceProperties.Join(destinationProperties, x => x.Name, y => y.Name, (x, y) => (sp: x, dp: y)).ToList();

                    sb2.AppendLine("\t\t// Source: " + Source.ToDisplayString() + ", Destination: " + Destination.ToDisplayString());
                    sb2.AppendLine($"\t\tpublic void MapTo{Destination.Name}({Source.ToDisplayString()} source, {Destination.ToDisplayString()} destination)");
                    sb2.AppendLine("\t\t{");

                    foreach (var (sp, dp) in sameProperties)
                    {
                        sb2.AppendLine($"\t\t\tdestination.{dp.Name} = source.{sp.Name};");
                    }

                    sb2.AppendLine("\t\t}");
                    sb2.AppendLine();

                    sb2.AppendLine($"\t\tpublic {Destination.ToDisplayString()} MapTo{Destination.Name}({Source.ToDisplayString()} source)");
                    sb2.AppendLine("\t\t{");
                    sb2.AppendLine("\t\t\treturn new " + Destination.ToDisplayString() + "()");
                    sb2.AppendLine("\t\t\t\t{");

                    foreach (var (sp, dp) in sameProperties)
                    {
                        sb2.AppendLine($"\t\t\t\t\t{dp.Name} = source.{sp.Name},");
                    }
                    sb2.AppendLine("\t\t\t\t};");
                    sb2.AppendLine("\t\t}");
                    sb2.AppendLine();

                    sb2.AppendLine($"\t\tpublic IEnumerable<{Destination.ToDisplayString()}> MapTo{Destination.Name}(IEnumerable<{Source.ToDisplayString()}> source)");
                    sb2.AppendLine("\t\t{");
                    sb2.AppendLine("\t\t\tforeach(var item in source)");
                    sb2.AppendLine("\t\t\t{");
                    sb2.AppendLine("\t\t\t\tyield return MapTo" + Destination.Name + "(item);");
                    sb2.AppendLine("\t\t\t};");
                    sb2.AppendLine("\t\t}");
                    sb2.AppendLine();

                }


                sb2.AppendLine("\t}");
                sb2.AppendLine("}");
                sb.AppendLine("//    " + mapperName);
                sb.AppendLine();

                context.AddSource("M_" + mapperName + ".g.cs", sb2.ToString());

            }

            context.AddSource("Generated.g.cs", sb.ToString());

            // Find the main method
            //            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

            //            // Build up the source code
            //            string source = $@"// <auto-generated/>
            //using System;

            //namespace {mainMethod.ContainingNamespace.ToDisplayString()}
            //{{
            //    public static partial class {mainMethod.ContainingType.Name}
            //    {{
            //        static partial void HelloFrom(string name) =>
            //            Console.WriteLine($""Generator says: Hi from '{{name}}'"");
            //    }}
            //}}
            //";
            //            var typeName = mainMethod.ContainingType.Name;

            //            // Add the source code to the compilation
            //            context.AddSource($"{typeName}.g.cs", source);
        }





        class StaticMapperSyntaxReceiver : ISyntaxContextReceiver
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
}