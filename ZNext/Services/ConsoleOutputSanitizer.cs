using System.Text.RegularExpressions;

namespace ZNext.Services;

internal static class ConsoleOutputSanitizer
{
	private static readonly Regex AnsiEscapeRegex = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]", RegexOptions.Compiled);
	private static readonly Regex LiteralColorCodeRegex = new(@"\[[0-9;]{1,16}m", RegexOptions.Compiled);
	private static readonly Regex AnsiOscRegex = new(@"\x1B\].*?(\x07|\x1B\\)", RegexOptions.Compiled);

	public static string SanitizeLine(string line)
	{
		if (string.IsNullOrEmpty(line))
		{
			return string.Empty;
		}

		string sanitized = AnsiEscapeRegex.Replace(line, string.Empty);
		sanitized = AnsiOscRegex.Replace(sanitized, string.Empty);
		sanitized = LiteralColorCodeRegex.Replace(sanitized, string.Empty);
		return sanitized.Replace("\0", string.Empty).TrimEnd('\r', '\n');
	}
}
