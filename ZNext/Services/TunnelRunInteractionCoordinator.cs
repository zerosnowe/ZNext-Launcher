namespace ZNext.Services;

internal sealed class TunnelRunInteractionCoordinator(
	TunnelRunCoordinator tunnelRunCoordinator,
	ConsoleSectionCoordinator consoleSectionCoordinator,
	Action selectSettings,
	Action selectConsole,
	Action showConsole)
{
	public async Task<bool> StartAsync(TunnelInfo tunnel, bool showConsolePanel)
	{
		TunnelRunStartResult result = await tunnelRunCoordinator.StartAsync(
			tunnel,
			showConsolePanel,
			CreateRunContext());
		ApplyStartResult(result);
		return result.Success;
	}

	public Task<bool> StopAsync(TunnelInfo tunnel)
	{
		return tunnelRunCoordinator.StopAsync(consoleSectionCoordinator.Sessions, tunnel);
	}

	public TunnelRunContext CreateRunContext()
	{
		return consoleSectionCoordinator.CreateTunnelRunContext();
	}

	public void ApplyStartResult(TunnelRunStartResult result)
	{
		if (result.ShouldNavigateToSettings)
		{
			selectSettings();
		}

		if (result.ShouldShowConsolePanel)
		{
			selectConsole();
			showConsole();
		}
	}
}
