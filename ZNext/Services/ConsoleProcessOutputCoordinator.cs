namespace ZNext.Services;

internal sealed class ConsoleProcessOutputCoordinator
{
	public ConsoleProcessOutputResult ProcessOutput(ConsoleSession session, string? rawLine)
	{
		if (string.IsNullOrEmpty(rawLine))
		{
			return ConsoleProcessOutputResult.Ignored();
		}

		string outputLine = ConsoleOutputSanitizer.SanitizeLine(rawLine);
		if (string.IsNullOrEmpty(outputLine))
		{
			return ConsoleProcessOutputResult.Ignored();
		}

		if (ConsoleOutputClassifier.TryExtractPromptDirectory(outputLine, out string promptDirectory))
		{
			if (!string.IsNullOrWhiteSpace(promptDirectory))
			{
				session.WorkingDirectory = promptDirectory;
				session.CurrentPrompt = "PS " + promptDirectory + ">";
			}

			return ConsoleProcessOutputResult.Prompt(session.CurrentPrompt);
		}

		if (ConsoleOutputClassifier.IsIssuedCommandEcho(outputLine, session.LastIssuedCommand))
		{
			session.LastIssuedCommand = null;
			return ConsoleProcessOutputResult.Ignored();
		}

		if (ConsoleOutputClassifier.IsInternalConsoleEcho(outputLine))
		{
			return ConsoleProcessOutputResult.Ignored();
		}

		bool isTunnelReady = session.IsTunnelSession
			&& !session.HasTunnelReadyNotified
			&& ConsoleOutputClassifier.IsTunnelReadyOutput(outputLine);

		if (isTunnelReady)
		{
			session.HasTunnelReadyNotified = true;
			session.TunnelAutoRestartAttempts = 0;
		}

		return ConsoleProcessOutputResult.Output(outputLine, isTunnelReady);
	}

	public string SanitizeError(string? rawLine)
	{
		return string.IsNullOrEmpty(rawLine)
			? string.Empty
			: ConsoleOutputSanitizer.SanitizeLine(rawLine);
	}
}

internal sealed record ConsoleProcessOutputResult(
	bool ShouldAppend,
	string Line,
	bool PromptUpdated,
	bool TunnelReady)
{
	public static ConsoleProcessOutputResult Ignored()
	{
		return new ConsoleProcessOutputResult(false, string.Empty, false, false);
	}

	public static ConsoleProcessOutputResult Prompt(string prompt)
	{
		return new ConsoleProcessOutputResult(true, prompt, true, false);
	}

	public static ConsoleProcessOutputResult Output(string line, bool tunnelReady)
	{
		return new ConsoleProcessOutputResult(true, line, false, tunnelReady);
	}
}
