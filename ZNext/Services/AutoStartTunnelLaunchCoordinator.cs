using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZNext.Services;

internal sealed class AutoStartTunnelLaunchCoordinator
{
	private readonly AutoStartTunnelSettingsService _settingsService;

	public AutoStartTunnelLaunchCoordinator(AutoStartTunnelSettingsService settingsService)
	{
		_settingsService = settingsService;
	}

	public async Task<AutoStartTunnelLaunchResult> TryLaunchAsync(
		bool isSignedIn,
		Func<Task> loadTunnelsAsync,
		Func<bool> hasCachedTunnels,
		Func<IEnumerable<TunnelInfo>> getTunnels,
		Func<TunnelInfo, Task<bool>> startTunnelAsync)
	{
		if (!_settingsService.IsEnabled || !isSignedIn)
		{
			return AutoStartTunnelLaunchResult.Skipped();
		}

		HashSet<int> selectedTunnelIds = _settingsService.LoadTunnelIds();
		if (selectedTunnelIds.Count == 0)
		{
			return AutoStartTunnelLaunchResult.WithSelection(selectedTunnelIds);
		}

		await loadTunnelsAsync();
		if (!hasCachedTunnels())
		{
			return AutoStartTunnelLaunchResult.WithSelection(selectedTunnelIds);
		}

		IReadOnlyList<TunnelInfo> targets = _settingsService.GetStartupTargets(getTunnels(), selectedTunnelIds);
		if (targets.Count == 0)
		{
			return AutoStartTunnelLaunchResult.WithSelection(selectedTunnelIds);
		}

		int startedCount = 0;
		foreach (TunnelInfo tunnel in targets)
		{
			if (await startTunnelAsync(tunnel))
			{
				startedCount++;
			}

			await Task.Delay(80);
		}

		return new AutoStartTunnelLaunchResult(selectedTunnelIds, startedCount, targets.Count);
	}
}

internal sealed record AutoStartTunnelLaunchResult(HashSet<int>? SelectedTunnelIds, int StartedCount, int TargetCount)
{
	public bool HasSelectionSnapshot => SelectedTunnelIds != null;

	public bool ShouldShowToast => TargetCount > 0;

	public string ToastMessage => $"已自动启动 {StartedCount}/{TargetCount} 条隧道";

	public static AutoStartTunnelLaunchResult Skipped()
	{
		return new AutoStartTunnelLaunchResult(null, 0, 0);
	}

	public static AutoStartTunnelLaunchResult WithSelection(HashSet<int> selectedTunnelIds)
	{
		return new AutoStartTunnelLaunchResult(selectedTunnelIds, 0, 0);
	}
}
