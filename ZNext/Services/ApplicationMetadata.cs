using System.IO;

namespace ZNext.Services;

internal static class ApplicationMetadata
{
	public const string DisplayName = "ZNext Launcher";

	public static string? ResolveIconPath()
	{
		string[] candidates =
		{
			Path.Combine(AppContext.BaseDirectory, "Assets", "ZNext.ico"),
			Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "ZNext.ico"),
			Path.Combine(Path.GetDirectoryName(typeof(ApplicationMetadata).Assembly.Location) ?? string.Empty, "Assets", "ZNext.ico"),
			Path.Combine(Environment.CurrentDirectory, "Assets", "ZNext.ico")
		};

		foreach (string candidate in candidates)
		{
			if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
			{
				return candidate;
			}
		}

		return null;
	}
}
