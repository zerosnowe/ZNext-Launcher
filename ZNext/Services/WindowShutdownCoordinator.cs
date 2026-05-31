using System.Diagnostics;

namespace ZNext.Services;

internal sealed class WindowShutdownCoordinator
{
	private readonly ConsoleSessionProcessService _consoleSessionProcessService;
	private readonly FrpcManagerService _frpcManagerService;
	private readonly ProcessLifetimeService _processLifetimeService;
	private readonly AppNotificationService _appNotificationService;
	private readonly HttpService _apiHttpService;
	private readonly TopSuccessToastController _topSuccessToastController;
	private bool _isExitTunnelShutdownInProgress;
	private bool _hasClosedTunnelSwitchesOnExit;

	public WindowShutdownCoordinator(
		ConsoleSessionProcessService consoleSessionProcessService,
		FrpcManagerService frpcManagerService,
		ProcessLifetimeService processLifetimeService,
		AppNotificationService appNotificationService,
		HttpService apiHttpService,
		TopSuccessToastController topSuccessToastController)
	{
		_consoleSessionProcessService = consoleSessionProcessService;
		_frpcManagerService = frpcManagerService;
		_processLifetimeService = processLifetimeService;
		_appNotificationService = appNotificationService;
		_apiHttpService = apiHttpService;
		_topSuccessToastController = topSuccessToastController;
	}

	public bool TryBeginClosePreparation()
	{
		if (_hasClosedTunnelSwitchesOnExit || _isExitTunnelShutdownInProgress)
		{
			return false;
		}

		_isExitTunnelShutdownInProgress = true;
		return true;
	}

	public async Task CompleteClosePreparationAsync(IEnumerable<TunnelInfo> tunnels)
	{
		try
		{
			await CloseAllTunnelSwitchesOnExitAsync(tunnels);
		}
		catch (Exception ex)
		{
			Debug.WriteLine("CloseAllTunnelsAndExitAsync failed: " + ex.Message);
		}
		finally
		{
			_isExitTunnelShutdownInProgress = false;
			_hasClosedTunnelSwitchesOnExit = true;
		}
	}

	public void CleanupResources(
		ICollection<ConsoleSession> sessions,
		ConsoleProcessHandlers processHandlers,
		Action detachNotificationHandler,
		Action resetConsoleState)
	{
		try
		{
			foreach (ConsoleSession session in sessions)
			{
				_consoleSessionProcessService.StopImmediately(session, processHandlers);
			}

			_frpcManagerService.KillResidualProcesses();
		}
		catch
		{
		}
		finally
		{
			foreach (ConsoleSession session in sessions)
			{
				_consoleSessionProcessService.DisposeProcess(session, processHandlers);
			}

			sessions.Clear();
			resetConsoleState();
			_processLifetimeService.Dispose();
			detachNotificationHandler();
			_appNotificationService.Dispose();
			_apiHttpService.Dispose();
			_topSuccessToastController.Dispose();
		}
	}

	private static Task CloseAllTunnelSwitchesOnExitAsync(IEnumerable<TunnelInfo> tunnels)
	{
		foreach (TunnelInfo tunnel in tunnels.Where(tunnel => tunnel.IsOnlineResolved))
		{
			tunnel.isOnline = false;
		}

		return Task.CompletedTask;
	}
}
