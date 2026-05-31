using System.Collections.Generic;

namespace ZNext.Services;

internal sealed class AutoStartSettingsCoordinator
{
	private readonly AutoStartApplicationService _applicationService;
	private readonly AutoStartTunnelSettingsService _tunnelSettingsService;

	public AutoStartSettingsCoordinator(
		AutoStartApplicationService applicationService,
		AutoStartTunnelSettingsService tunnelSettingsService)
	{
		_applicationService = applicationService;
		_tunnelSettingsService = tunnelSettingsService;
	}

	public bool IsAutoStartLaunch(string[] args)
	{
		return _applicationService.IsAutoStartLaunch(args);
	}

	public bool IsApplicationAutoStartEnabled()
	{
		return _applicationService.IsEnabled();
	}

	public AutoStartChangeResult SetApplicationAutoStartEnabled(bool enabled)
	{
		return _applicationService.SetEnabled(enabled);
	}

	public AutoStartTunnelSettingsSnapshot LoadTunnelSettings()
	{
		return new AutoStartTunnelSettingsSnapshot(
			_tunnelSettingsService.LoadTunnelIds(),
			_tunnelSettingsService.IsEnabled);
	}

	public AutoStartTunnelToggleResult SetTunnelAutoStartEnabled(bool enabled)
	{
		_tunnelSettingsService.SetEnabled(enabled);
		return new AutoStartTunnelToggleResult(enabled ? "已开启隧道随应用启动" : "已关闭隧道随应用启动");
	}

	public IReadOnlyList<AutoStartTunnelChecklistItem> BuildChecklistItems(
		IEnumerable<TunnelInfo> tunnels,
		IReadOnlySet<int> selectedTunnelIds)
	{
		return _tunnelSettingsService.BuildChecklistItems(tunnels, selectedTunnelIds);
	}

	public void UpdateTunnelSelection(ISet<int> selectedTunnelIds, int tunnelId, bool isSelected)
	{
		_tunnelSettingsService.UpdateTunnelSelection(selectedTunnelIds, tunnelId, isSelected);
	}
}

internal sealed record AutoStartTunnelSettingsSnapshot(HashSet<int> SelectedTunnelIds, bool IsEnabled);

internal sealed record AutoStartTunnelToggleResult(string Message);
