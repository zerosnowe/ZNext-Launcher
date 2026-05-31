using System.Text;

namespace ZNext.Services;

internal sealed class TunnelRunCoordinator
{
	private readonly UserSessionService _userSessionService;
	private readonly UserDialogService _userDialogService;
	private readonly FrpcSettingsService _frpcSettingsService;
	private readonly TunnelService _tunnelService;
	private readonly TunnelConsoleSessionService _tunnelConsoleSessionService;
	private readonly TunnelSessionCoordinator _tunnelSessionCoordinator;
	private readonly Action<string> _showSuccessToast;

	public TunnelRunCoordinator(
		UserSessionService userSessionService,
		UserDialogService userDialogService,
		FrpcSettingsService frpcSettingsService,
		TunnelService tunnelService,
		TunnelConsoleSessionService tunnelConsoleSessionService,
		TunnelSessionCoordinator tunnelSessionCoordinator,
		Action<string> showSuccessToast)
	{
		_userSessionService = userSessionService;
		_userDialogService = userDialogService;
		_frpcSettingsService = frpcSettingsService;
		_tunnelService = tunnelService;
		_tunnelConsoleSessionService = tunnelConsoleSessionService;
		_tunnelSessionCoordinator = tunnelSessionCoordinator;
		_showSuccessToast = showSuccessToast;
	}

	public async Task<TunnelRunStartResult> StartAsync(
		TunnelInfo tunnel,
		bool showConsolePanel,
		TunnelRunContext context)
	{
		if (!_userSessionService.IsSignedIn)
		{
			await _userDialogService.ShowInfoAsync("提示", "请先登录后再启动隧道。");
			return TunnelRunStartResult.Failed();
		}

		TunnelRunStartResult frpcResult = await EnsureFrpcCoreInstalledAsync();
		if (!frpcResult.Success)
		{
			return frpcResult;
		}

		try
		{
			_userSessionService.SynchronizeTokens();
			TunnelStartCommandResult commandResult = await _tunnelService.GetStartCommandAsync(tunnel.proxyId);
			if (!commandResult.Success || string.IsNullOrWhiteSpace(commandResult.Command))
			{
				await _userDialogService.ShowInfoAsync("启动失败", commandResult.Message);
				return TunnelRunStartResult.Failed();
			}

			context.MarkConsoleInitialized();
			ConsoleSession? targetSession = _tunnelSessionCoordinator.FindRunningSession(context.Sessions, tunnel.proxyId);
			if (targetSession != null)
			{
				_tunnelConsoleSessionService.RefreshDisplayMetadata(targetSession, tunnel);
				context.SwitchActiveSession(targetSession);
				_showSuccessToast("隧道 " + targetSession.TunnelDisplayName + " 已在运行");
				return TunnelRunStartResult.Started(showConsolePanel);
			}

			string workingDirectory = context.ResolveWorkingDirectory();
			targetSession = context.CreateTunnelSession($"隧道 {tunnel.proxyId}");
			_tunnelConsoleSessionService.ConfigureSession(targetSession, tunnel, commandResult.Command, workingDirectory);
			context.SwitchActiveSession(targetSession);
			_tunnelSessionCoordinator.StartProcess(
				targetSession,
				commandResult.Command,
				workingDirectory,
				context.Encoding,
				context.CreateProcessHandlers(),
				context.AppendConsoleOutput);
			return TunnelRunStartResult.Started(showConsolePanel);
		}
		catch (Exception ex)
		{
			await _userDialogService.ShowInfoAsync("启动失败", ex.Message);
			return TunnelRunStartResult.Failed();
		}
	}

	public Task<bool> StopAsync(IEnumerable<ConsoleSession> sessions, TunnelInfo tunnel)
	{
		return _tunnelSessionCoordinator.StopAsync(sessions, tunnel);
	}

	public async Task<TunnelRunToggleResult> ToggleAsync(
		TunnelInfo tunnel,
		bool isOn,
		TunnelRunContext context)
	{
		if (isOn)
		{
			TunnelRunStartResult startResult = await StartAsync(tunnel, showConsolePanel: true, context);
			return TunnelRunToggleResult.FromStart(startResult);
		}

		if (await StopAsync(context.Sessions, tunnel))
		{
			return TunnelRunToggleResult.Stopped();
		}

		await _userDialogService.ShowInfoAsync("停止失败", "未能终止该隧道进程，请到控制台手动 Ctrl+C。");
		return TunnelRunToggleResult.StopFailed();
	}

	private async Task<TunnelRunStartResult> EnsureFrpcCoreInstalledAsync()
	{
		if (_frpcSettingsService.HasInstalledExecutable())
		{
			return TunnelRunStartResult.Started(showConsolePanel: false);
		}

		bool shouldNavigateToSettings = await _userDialogService.ShowPrimaryAsync(
			"未安装 frpc 核心",
			"检测到未安装 mefrpc，请前往“设置”页面安装 frpc 核心后再启动隧道。",
			"前往设置");
		return shouldNavigateToSettings
			? TunnelRunStartResult.NeedsSettings()
			: TunnelRunStartResult.Failed();
	}
}

internal sealed record TunnelRunContext(
	IEnumerable<ConsoleSession> Sessions,
	Func<string?, ConsoleSession> CreateTunnelSession,
	Func<string> ResolveWorkingDirectory,
	Encoding Encoding,
	Func<ConsoleProcessHandlers> CreateProcessHandlers,
	Action<ConsoleSession, string> AppendConsoleOutput,
	Action<ConsoleSession> SwitchActiveSession,
	Action MarkConsoleInitialized);

internal sealed record TunnelRunStartResult(
	bool Success,
	bool ShouldShowConsolePanel,
	bool ShouldNavigateToSettings)
{
	public static TunnelRunStartResult Started(bool showConsolePanel)
	{
		return new TunnelRunStartResult(true, showConsolePanel, false);
	}

	public static TunnelRunStartResult Failed()
	{
		return new TunnelRunStartResult(false, false, false);
	}

	public static TunnelRunStartResult NeedsSettings()
	{
		return new TunnelRunStartResult(false, false, true);
	}
}

internal sealed record TunnelRunToggleResult(
	TunnelRunStartResult StartResult,
	bool ShouldRollbackToggle,
	bool RollbackToggleIsOn,
	bool ShouldReloadCards)
{
	public static TunnelRunToggleResult FromStart(TunnelRunStartResult startResult)
	{
		return new TunnelRunToggleResult(
			startResult,
			ShouldRollbackToggle: !startResult.Success,
			RollbackToggleIsOn: false,
			ShouldReloadCards: startResult.Success);
	}

	public static TunnelRunToggleResult Stopped()
	{
		return new TunnelRunToggleResult(
			TunnelRunStartResult.Failed(),
			ShouldRollbackToggle: false,
			RollbackToggleIsOn: false,
			ShouldReloadCards: true);
	}

	public static TunnelRunToggleResult StopFailed()
	{
		return new TunnelRunToggleResult(
			TunnelRunStartResult.Failed(),
			ShouldRollbackToggle: true,
			RollbackToggleIsOn: true,
			ShouldReloadCards: false);
	}
}
