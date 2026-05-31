using System;

namespace ZNext.Services;

internal static class ConsoleOutputClassifier
{
	private const string PromptMarker = "__ZNEXT_PROMPT__:";

	public static bool TryExtractPromptDirectory(string outputLine, out string workingDirectory)
	{
		workingDirectory = string.Empty;
		if (!outputLine.StartsWith(PromptMarker, StringComparison.Ordinal))
		{
			return false;
		}

		workingDirectory = outputLine.Substring(PromptMarker.Length).Trim();
		return true;
	}

	public static bool IsIssuedCommandEcho(string outputLine, string? lastIssuedCommand)
	{
		if (string.IsNullOrWhiteSpace(lastIssuedCommand))
		{
			return false;
		}

		string trimmed = outputLine.Trim();
		return string.Equals(trimmed, lastIssuedCommand, StringComparison.Ordinal)
			|| (outputLine.TrimStart().StartsWith("PS ", StringComparison.OrdinalIgnoreCase)
				&& outputLine.TrimEnd().EndsWith(lastIssuedCommand, StringComparison.Ordinal));
	}

	public static bool IsInternalConsoleEcho(string outputLine)
	{
		string text = outputLine.Trim();
		if (!text.StartsWith("PS ", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string normalized = text.ToLowerInvariant();
		return normalized.Contains("set-location -literalpath")
			|| normalized.Contains("write-output \"__znext_prompt__");
	}

	public static bool IsTunnelReadyOutput(string outputLine)
	{
		if (string.IsNullOrWhiteSpace(outputLine))
		{
			return false;
		}

		string text = outputLine.Trim().ToLowerInvariant();
		return text.Contains("start proxy success", StringComparison.Ordinal)
			|| text.Contains("login to server success", StringComparison.Ordinal)
			|| text.Contains("proxy added", StringComparison.Ordinal)
			|| text.Contains("隧道启动成功", StringComparison.Ordinal)
			|| text.Contains("启动成功", StringComparison.Ordinal);
	}
}
