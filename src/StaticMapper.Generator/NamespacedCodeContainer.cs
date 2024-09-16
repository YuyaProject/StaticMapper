using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Runtime.InteropServices;
using System.Text;

namespace StaticMapper.Generator;

public class NamespacedCodeContainer
{
	public static readonly string NewLine = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "\r\n" : "\n";

	public NamespacedCodeContainer(int runId, string fileName, string baseNamespaceName, IEnumerable<string>? usings = null)
	{
		RunId = runId;
		FileName = fileName;
		BaseNamespaceName = baseNamespaceName;
		Prefix = IsGlobalNamespace ? "" : "\t";

		if (usings != null) AddGlobalUsingRange(usings);
	}

	private NamespacedCodeContainer(int runId, string fileName, string baseNamespaceName, string prefix)
	{
		RunId = runId;
		FileName = fileName;
		BaseNamespaceName = baseNamespaceName;
		Prefix = prefix;
	}

	public HashSet<string> GlobalUsings { get; } = [];

	public Dictionary<string, NamespacedCodeContainer> NamespaceBodies { get; } = [];

	public StringBuilder Body { get; } = new StringBuilder();

	public int RunId { get; }

	public string FileName { get; }

	public string BaseNamespaceName { get; }

	public string Prefix { get; }

	public bool IsGlobalNamespace => string.IsNullOrWhiteSpace(BaseNamespaceName) || BaseNamespaceName == "global" || BaseNamespaceName == "global::";

	public NamespacedCodeContainer AddOrGetNamespacedCodeContainer(string namespaceName)
	{
		var baseNamespace = IsGlobalNamespace ? string.Empty : BaseNamespaceName.Trim('.');

		var splitedNamespace = (string.IsNullOrEmpty(baseNamespace)
									? namespaceName
									: namespaceName.Substring(baseNamespace.Length + 1))
							   .Split('.');
		if (!NamespaceBodies.TryGetValue(splitedNamespace[0], out var namespacedCodeContainer))
		{
			namespacedCodeContainer = new NamespacedCodeContainer(RunId, FileName, $"{baseNamespace}.{splitedNamespace[0]}", Prefix + "\t");
			NamespaceBodies.Add(splitedNamespace[0], namespacedCodeContainer);
		}

		return splitedNamespace.Length == 1
			? namespacedCodeContainer
			: namespacedCodeContainer.AddOrGetNamespacedCodeContainer(namespaceName);
	}

	public NamespacedCodeContainer AddEmptyLine()
	{
		Body.AppendLine();
		return this;
	}

	public NamespacedCodeContainer AddSingleCodeLine(string codeText)
	{
		Body.Append(Prefix);
		if (codeText == null) return this;
		Body.AppendLine(codeText);
		return this;
	}

	public NamespacedCodeContainer AddBeginOfCodeLine(object codeText)
	{
		Body.Append(Prefix);
		if (codeText == null) return this;
		Body.Append(codeText);
		return this;
	}

	public NamespacedCodeContainer AddEndOfCodeLine(string codeText)
	{
		Body.AppendLine(codeText);
		return this;
	}

	public NamespacedCodeContainer AddCode(object codeText)
	{
		Body.Append(codeText);
		return this;
	}
	public NamespacedCodeContainer AddMultilinedCodeText(string codeText)
	{
		if (string.IsNullOrWhiteSpace(codeText)) return this;
		var t = codeText.Replace(NewLine, "\n").Split('\n');
		if (!string.IsNullOrWhiteSpace(t[0]))
		{
			Body.Append(Prefix);
			Body.AppendLine(t[0]);
		}
		else
		{
			Body.AppendLine();
		}

		foreach (var item in t.Skip(1))
		{
			if (string.IsNullOrWhiteSpace(item))
			{
				Body.AppendLine();
			}
			else
			{
				Body.Append(Prefix);
				Body.AppendLine(item);
			}
		}
		return this;
	}

	public NamespacedCodeContainer AddGlobalUsing(string usingNamespace)
	{
		if (string.IsNullOrWhiteSpace(usingNamespace)) return this;
		GlobalUsings.Add($"using {usingNamespace.Trim()};");
		return this;
	}

	public NamespacedCodeContainer AddGlobalUsingRange(IEnumerable<string> usingNamespaces)
	{
		foreach (var item in usingNamespaces) AddGlobalUsing(item);
		return this;
	}

	private IEnumerable<string> GetGlobalUsings()
	{
		return GlobalUsings
			.Union(NamespaceBodies.SelectMany(x => x.Value.GetGlobalUsings()));
	}

	public override string ToString()
	{
		var sb = new StringBuilder(CreateStringBuilderForCode());

		var allGlobalUsings = GetGlobalUsings()
			.OrderBy(x => x)
			.Distinct()
			.ToList();
		if (allGlobalUsings.Count > 0)
		{
			sb.AppendLine($"{NewLine}// Usings :");
			foreach (var item in allGlobalUsings)
			{
				sb.AppendLine(item);
			}
		}

		if (!IsGlobalNamespace)
		{
			sb.AppendLine($"\r\nnamespace {BaseNamespaceName}\r\n{{");
			AddBodyAndChildrenNamespaces(sb);
			sb.AppendLine("}");
		}
		else
		{
			AddBodyAndChildrenNamespaces(sb);
		}

		return sb.ToString();
	}

	private void AddBodyAndChildrenNamespaces(StringBuilder sb)
	{
		AddBodyText(sb);

		if (NamespaceBodies.Count == 0)
		{
			return;
		}

		AddSubNamespaceTexts(sb);
	}

	private void AddSubNamespaceTexts(StringBuilder sb)
	{
		foreach (var item in NamespaceBodies.OrderBy(x => x.Key))
		{
			sb.AppendLine();
			sb.Append(Prefix);
			sb.Append("namespace ");
			sb.AppendLine(item.Key);
			sb.Append(Prefix);
			sb.AppendLine("{");

			item.Value.AddBodyAndChildrenNamespaces(sb);

			sb.Append(Prefix);
			sb.AppendLine("}");
		}
	}

	private void AddBodyText(StringBuilder sb)
	{
		if (Body.Length <= 0)
		{
			return;
		}

		sb.Append(Body);
	}

	private string CreateStringBuilderForCode()
	{
		return $@"// ----------------------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}
//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ----------------------------------------------------------------------------------------------
// File Name	: {FileName}
// Compile Time : {DateTime.Now}
// Counter		: {RunId}
";
	}

	public void WriteCodeToFile(GeneratorExecutionContext context)
	{
		context.AddSource(FileName, SourceText.From(this.ToString(), Encoding.UTF8));
	}
}
