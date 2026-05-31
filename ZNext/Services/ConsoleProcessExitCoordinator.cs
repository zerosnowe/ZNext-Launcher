namespace ZNext.Services;

internal sealed class ConsoleProcessExitCoordinator
{
	private readonly ConsoleSessionProcessService _processService;
	private readonly TunnelConsoleSessionService _tunnelConsoleSessionService;

	public ConsoleProcessExitCoordinator(
		ConsoleSessionProcessService processService,
		TunnelConsoleSessionService tunnelConsoleSessionService)
	{
		_processService = processService;
		_tunnelConsoleSessionService = tunnelConsoleSessionService;
	}

	public ConsoleProcessExitResult ProcessExit(ConsoleSession session, bool isWindowClosing)
	{
		int exitCode = _processService.GetExitCode(session);
		string exitLine = session.IsTunnelSession
			? $"[隧道进程已退出] ExitCode={exitCode}"
			: "[PowerShell 已退出]";

		if (!session.IsTunnelSession || !_tunnelConsoleSessionService.CanScheduleAutoRestart(session, isWindowClosing))
		{
			return new ConsoleProcessExitResult(exitLine, string.Empty, false);
		}

		session.TunnelAutoRestartAttempts++;
		string restartLine = $"[隧道进程异常退出] 正在尝试重连({session.TunnelAutoRestartAttempts}/{TunnelConsoleSessionService.MaxAutoRestartAttempts})...";
		return new ConsoleProcessExitResult(exitLine, restartLine, true);
	}

	public bool CanRunScheduledRestart(ConsoleSession session, bool isWindowClosing)
	{
		return _tunnelConsoleSessionService.CanRunAutoRestart(session, isWindowClosing)
			&& !string.IsNullOrWhiteSpace(session.TunnelStartCommand)
			&& !_processService.IsRunning(session);
	}
}

internal sealed record ConsoleProcessExitResult(
	string ExitLine,
	string RestartLine,
	bool ShouldScheduleRestart);
