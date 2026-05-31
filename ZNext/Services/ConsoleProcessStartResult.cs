namespace ZNext.Services;

internal sealed class ConsoleProcessStartResult
{
	public bool Success { get; init; }

	public int ProcessId { get; init; }

	public string? ErrorMessage { get; init; }

	public static ConsoleProcessStartResult FromSuccess(int processId)
	{
		return new ConsoleProcessStartResult
		{
			Success = true,
			ProcessId = processId
		};
	}

	public static ConsoleProcessStartResult FromError(string message)
	{
		return new ConsoleProcessStartResult
		{
			Success = false,
			ErrorMessage = message
		};
	}
}
