using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace ZNext.Services;

internal sealed class TunnelCardInteractionCoordinator
{
	private readonly TunnelRunCoordinator _tunnelRunCoordinator;
	private readonly TunnelMenuActionCoordinator _tunnelMenuActionCoordinator;
	private readonly Func<TunnelRunContext> _createRunContext;
	private readonly Func<bool> _isRefreshingTunnelCards;
	private readonly Func<Task> _reloadTunnelCardsAsync;
	private readonly Action _markTunnelsNotLoaded;
	private readonly Func<Task> _loadTunnelsAsync;
	private readonly Action<TunnelRunStartResult> _applyRunStartResult;
	private readonly HashSet<ToggleSwitch> _initializedRunSwitches = new HashSet<ToggleSwitch>();
	private bool _isUpdatingRunSwitch;

	public TunnelCardInteractionCoordinator(
		TunnelRunCoordinator tunnelRunCoordinator,
		TunnelMenuActionCoordinator tunnelMenuActionCoordinator,
		Func<TunnelRunContext> createRunContext,
		Func<bool> isRefreshingTunnelCards,
		Func<Task> reloadTunnelCardsAsync,
		Action markTunnelsNotLoaded,
		Func<Task> loadTunnelsAsync,
		Action<TunnelRunStartResult> applyRunStartResult)
	{
		_tunnelRunCoordinator = tunnelRunCoordinator;
		_tunnelMenuActionCoordinator = tunnelMenuActionCoordinator;
		_createRunContext = createRunContext;
		_isRefreshingTunnelCards = isRefreshingTunnelCards;
		_reloadTunnelCardsAsync = reloadTunnelCardsAsync;
		_markTunnelsNotLoaded = markTunnelsNotLoaded;
		_loadTunnelsAsync = loadTunnelsAsync;
		_applyRunStartResult = applyRunStartResult;
	}

	public void HandleRunToggleLoaded(object sender)
	{
		if (sender is not ToggleSwitch toggleSwitch)
		{
			return;
		}

		_initializedRunSwitches.Add(toggleSwitch);
		toggleSwitch.Unloaded -= HandleRunToggleUnloaded;
		toggleSwitch.Unloaded += HandleRunToggleUnloaded;
	}

	public async Task HandleRunToggleToggledAsync(object sender)
	{
		if (_isRefreshingTunnelCards() || _isUpdatingRunSwitch)
		{
			return;
		}

		if (sender is not ToggleSwitch toggleSwitch
			|| !_initializedRunSwitches.Contains(toggleSwitch)
			|| toggleSwitch.Tag is not TunnelInfo tunnel)
		{
			return;
		}

		TunnelRunToggleResult result = await _tunnelRunCoordinator.ToggleAsync(tunnel, toggleSwitch.IsOn, _createRunContext());
		_applyRunStartResult(result.StartResult);
		if (result.ShouldRollbackToggle)
		{
			SetRunToggleSilently(toggleSwitch, result.RollbackToggleIsOn);
		}

		if (result.ShouldReloadCards)
		{
			await _reloadTunnelCardsAsync();
		}
	}

	public async Task CopyLinkAsync(object sender)
	{
		if (sender is FrameworkElement { Tag: TunnelInfo tunnel })
		{
			await ApplyMenuActionResultAsync(await _tunnelMenuActionCoordinator.CopyLinkAsync(tunnel));
		}
	}

	public async Task ShowDetailsAsync(object sender)
	{
		TunnelInfo? tunnel = ExtractTunnelItemFromMenuSender(sender);
		if (tunnel != null)
		{
			await ApplyMenuActionResultAsync(await _tunnelMenuActionCoordinator.ShowDetailsAsync(tunnel));
		}
	}

	public async Task EditAsync(object sender)
	{
		TunnelInfo? tunnel = ExtractTunnelItemFromMenuSender(sender);
		if (tunnel != null)
		{
			await ApplyMenuActionResultAsync(await _tunnelMenuActionCoordinator.EditAsync(tunnel));
		}
	}

	public async Task EnableAsync(object sender)
	{
		TunnelInfo? tunnel = ExtractTunnelItemFromMenuSender(sender);
		if (tunnel != null)
		{
			await ApplyMenuActionResultAsync(await _tunnelMenuActionCoordinator.EnableAsync(tunnel));
		}
	}

	public async Task ForceOfflineAsync(object sender)
	{
		TunnelInfo? tunnel = ExtractTunnelItemFromMenuSender(sender);
		if (tunnel != null)
		{
			await ApplyMenuActionResultAsync(await _tunnelMenuActionCoordinator.ForceOfflineAsync(tunnel));
		}
	}

	public async Task DeleteAsync(object sender)
	{
		TunnelInfo? tunnel = ExtractTunnelItemFromMenuSender(sender);
		if (tunnel != null)
		{
			await ApplyMenuActionResultAsync(await _tunnelMenuActionCoordinator.DeleteAsync(tunnel));
		}
	}

	private void HandleRunToggleUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is ToggleSwitch toggleSwitch)
		{
			toggleSwitch.Unloaded -= HandleRunToggleUnloaded;
			_initializedRunSwitches.Remove(toggleSwitch);
		}
	}

	private void SetRunToggleSilently(ToggleSwitch toggleSwitch, bool isOn)
	{
		_isUpdatingRunSwitch = true;
		toggleSwitch.IsOn = isOn;
		_isUpdatingRunSwitch = false;
	}

	private async Task ApplyMenuActionResultAsync(TunnelMenuActionResult result)
	{
		switch (result.RefreshMode)
		{
			case TunnelMenuRefreshMode.ReloadCards:
				await _reloadTunnelCardsAsync();
				break;
			case TunnelMenuRefreshMode.LoadFresh:
				_markTunnelsNotLoaded();
				await _loadTunnelsAsync();
				break;
		}
	}

	private static TunnelInfo? ExtractTunnelItemFromMenuSender(object sender)
	{
		return (sender as MenuFlyoutItem)?.Tag as TunnelInfo;
	}
}
