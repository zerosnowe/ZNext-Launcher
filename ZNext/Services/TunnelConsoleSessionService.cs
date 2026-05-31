using System;
using System.Collections.Generic;
using System.Linq;

namespace ZNext.Services;

internal sealed class TunnelConsoleSessionService
{
	public const int MaxAutoRestartAttempts = 3;

	private readonly ConsoleSessionProcessService _processService;

	public TunnelConsoleSessionService(ConsoleSessionProcessService processService)
	{
		_processService = processService;
	}

	public ConsoleSession? FindRunningSession(IEnumerable<ConsoleSession> sessions, int proxyId)
	{
		return sessions.FirstOrDefault(session =>
			session.IsTunnelSession
			&& session.TunnelProxyId == proxyId
			&& _processService.IsRunning(session));
	}

	public void SyncLocalRunStates(IEnumerable<TunnelInfo> tunnels, IEnumerable<ConsoleSession> sessions)
	{
		foreach (TunnelInfo tunnel in tunnels)
		{
			tunnel.IsLocalRunning = FindRunningSession(sessions, tunnel.proxyId) != null;
		}
	}

	public void ConfigureSession(ConsoleSession session, TunnelInfo tunnel, string startCommand, string workingDirectory)
	{
		string title = ResolveTitle(tunnel);
		session.Title = title;
		session.IsTunnelSession = true;
		session.TunnelProxyId = tunnel.proxyId;
		session.TunnelDisplayName = title;
		session.WorkingDirectory = workingDirectory;
		session.HasTunnelReadyNotified = false;
		session.TunnelStopRequested = false;
		session.TunnelStartCommand = startCommand;
		session.TunnelAutoRestartAttempts = 0;
	}

	public void RefreshDisplayMetadata(ConsoleSession session, TunnelInfo tunnel)
	{
		string title = ResolveTitle(tunnel);
		session.Title = title;
		session.TunnelDisplayName = title;
		session.TunnelStopRequested = false;
	}

	public bool CanScheduleAutoRestart(ConsoleSession session, bool isWindowClosing)
	{
		return CanRunAutoRestart(session, isWindowClosing)
			&& session.TunnelAutoRestartAttempts < MaxAutoRestartAttempts;
	}

	public bool CanRunAutoRestart(ConsoleSession session, bool isWindowClosing)
	{
		return !isWindowClosing
			&& !session.IsClosing
			&& !session.TunnelStopRequested
			&& !string.IsNullOrWhiteSpace(session.TunnelStartCommand);
	}

	private static string ResolveTitle(TunnelInfo tunnel)
	{
		return string.IsNullOrWhiteSpace(tunnel.proxyName)
			? $"隧道 {tunnel.proxyId}"
			: tunnel.proxyName.Trim();
	}
}
