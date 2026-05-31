namespace ZNext.Services;

internal sealed class TunnelSessionCoordinator
{
	private readonly TunnelConsoleSessionService _tunnelConsoleSessionService;
	private readonly ConsoleSessionProcessService _consoleSessionProcessService;

	public TunnelSessionCoordinator(
		TunnelConsoleSessionService tunnelConsoleSessionService,
		ConsoleSessionProcessService consoleSessionProcessService)
	{
		_tunnelConsoleSessionService = tunnelConsoleSessionService;
		_consoleSessionProcessService = consoleSessionProcessService;
	}

	public ConsoleSession? FindRunningSession(IEnumerable<ConsoleSession> sessions, int proxyId)
	{
		return _tunnelConsoleSessionService.FindRunningSession(sessions, proxyId);
	}

	public void StartProcess(
		ConsoleSession session,
		string command,
		string fallbackWorkingDirectory,
		System.Text.Encoding encoding,
		ConsoleProcessHandlers handlers,
		Action<ConsoleSession, string> appendOutput)
	{
		if (string.IsNullOrWhiteSpace(command))
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(session.WorkingDirectory))
		{
			session.WorkingDirectory = fallbackWorkingDirectory;
		}

		ConsoleProcessStartResult result = _consoleSessionProcessService.StartTunnel(session, command, encoding, handlers);
		if (!result.Success)
		{
			appendOutput(session, "[隧道启动失败] " + result.ErrorMessage);
			return;
		}

		appendOutput(session, $"[隧道进程已启动] {DateTime.Now:HH:mm:ss} PID={result.ProcessId}");
	}

	public async Task<bool> StopAsync(IEnumerable<ConsoleSession> sessions, TunnelInfo tunnel)
	{
		ConsoleSession? runningTunnelSession = FindRunningSession(sessions, tunnel.proxyId);
		if (runningTunnelSession == null)
		{
			return true;
		}

		runningTunnelSession.TunnelStopRequested = true;
		try
		{
			if (!_consoleSessionProcessService.TryTerminateChildProcesses(runningTunnelSession))
			{
				bool terminated = _consoleSessionProcessService.TryTerminateMainProcess(runningTunnelSession);
				if (!terminated)
				{
					runningTunnelSession.TunnelStopRequested = false;
				}
				return terminated;
			}

			await Task.Delay(160);
			if (!_consoleSessionProcessService.IsRunning(runningTunnelSession))
			{
				return true;
			}

			if (_consoleSessionProcessService.TryTerminateMainProcess(runningTunnelSession)
				|| !_consoleSessionProcessService.IsRunning(runningTunnelSession))
			{
				return true;
			}

			runningTunnelSession.TunnelStopRequested = false;
			return false;
		}
		catch
		{
			runningTunnelSession.TunnelStopRequested = false;
			return false;
		}
	}
}
