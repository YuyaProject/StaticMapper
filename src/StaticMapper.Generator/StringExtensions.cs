using System.Globalization;

namespace StaticMapper.Generator;

public static class StringExtensions
{
	/// <summary>
	/// Converts string to kebab-case.
	/// </summary>
	/// <param name="source">The source.</param>
	/// <param name="cultureInfo">The culture information.</param>
	/// <returns>The kebab case string</returns>
	public static string? ToKebabCase(this string source, CultureInfo? cultureInfo = null)
	{
		if (source is null) return null;

		if (source.Length == 0) return string.Empty;

		cultureInfo ??= Thread.CurrentThread.CurrentCulture;

		if (source.Length == 1) return source.ToLower(cultureInfo);

		var newList = new List<char>(source!.Length + (source!.Length / 2)) { char.ToLower(source[0], cultureInfo) };

		for (var i = 1; i < source.Length; i++)
		{
			var s = source[i];
			var s2 = char.ToLower(s, cultureInfo);
			if (char.IsLower(s) || char.IsPunctuation(s)) // if current char is already lowercase
			{
				newList.Add(s);
			}
			else if (char.IsPunctuation(source[i - 1])) // if current char is upper and previous char is lower
			{
				newList.Add(s2);
			}
			else if (char.IsLower(source[i - 1])) // if current char is upper and previous char is lower
			{
				Add(s2, newList);
			}
			else if (char.IsUpper(source[i - 1]) && char.IsUpper(source[i]))
			{
				newList.Add(s2);
			}
			else if (i + 1 == source.Length || char.IsUpper(source[i + 1])) // if current char is upper and next char doesn't exist or is upper
			{
				newList.Add(s2);
			}
			else
			{
				Add(s2, newList);
			}
		}
		return new string([.. newList]);
	}
	private static void Add(char source, List<char> newList)
	{
		newList.Add('-');
		newList.Add(source);
	}
}