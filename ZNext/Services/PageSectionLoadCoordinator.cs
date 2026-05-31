using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class PageSectionLoadCoordinator
{
	private readonly PageDataLoadCoordinator _pageDataLoadCoordinator;
	private readonly PageLoadStateCoordinator _pageLoadStateCoordinator;
	private readonly TunnelsSectionViewModel _tunnelsViewModel;
	private readonly Action _ensureNodeSectionCreated;
	private readonly Func<bool> _hasVisibleTunnelData;
	private readonly Action<IEnumerable<TunnelInfo>> _syncTunnelLocalRunStates;
	private readonly Action _rebuildAutoStartChecklist;
	private readonly Action _applyTunnelFiltersAndLayout;
	private readonly Action _applyCreateTunnelNodeFilters;

	public PageSectionLoadCoordinator(
		PageDataLoadCoordinator pageDataLoadCoordinator,
		PageLoadStateCoordinator pageLoadStateCoordinator,
		TunnelsSectionViewModel tunnelsViewModel,
		Action ensureNodeSectionCreated,
		Func<bool> hasVisibleTunnelData,
		Action<IEnumerable<TunnelInfo>> syncTunnelLocalRunStates,
		Action rebuildAutoStartChecklist,
		Action applyTunnelFiltersAndLayout,
		Action applyCreateTunnelNodeFilters)
	{
		_pageDataLoadCoordinator = pageDataLoadCoordinator;
		_pageLoadStateCoordinator = pageLoadStateCoordinator;
		_tunnelsViewModel = tunnelsViewModel;
		_ensureNodeSectionCreated = ensureNodeSectionCreated;
		_hasVisibleTunnelData = hasVisibleTunnelData;
		_syncTunnelLocalRunStates = syncTunnelLocalRunStates;
		_rebuildAutoStartChecklist = rebuildAutoStartChecklist;
		_applyTunnelFiltersAndLayout = applyTunnelFiltersAndLayout;
		_applyCreateTunnelNodeFilters = applyCreateTunnelNodeFilters;
	}

	public bool IsRefreshingTunnelCards { get; private set; }

	public async Task LoadNodesAsync()
	{
		_ensureNodeSectionCreated();
		await _pageDataLoadCoordinator.LoadNodesAsync();
	}

	public async Task LoadTunnelsAsync(bool forceReload = false)
	{
		ApplyUiResult(await _pageDataLoadCoordinator.LoadTunnelsAsync(
			forceReload,
			_hasVisibleTunnelData(),
			_syncTunnelLocalRunStates));
	}

	public async Task ReloadTunnelCardsAsync()
	{
		IsRefreshingTunnelCards = true;
		try
		{
			_pageLoadStateCoordinator.ResetTunnels();
			_tunnelsViewModel.Clear();
			await LoadTunnelsAsync(forceReload: true);
		}
		finally
		{
			await Task.Delay(200);
			IsRefreshingTunnelCards = false;
		}
	}

	public async Task LoadCreateTunnelNodesAsync(bool forceReload = false)
	{
		ApplyUiResult(await _pageDataLoadCoordinator.LoadCreateTunnelNodesAsync(forceReload));
	}

	private void ApplyUiResult(PageDataLoadUiResult result)
	{
		if (result.ShouldRebuildAutoStartChecklist)
		{
			_rebuildAutoStartChecklist();
		}

		if (result.ShouldApplyTunnelFiltersAndLayout)
		{
			_applyTunnelFiltersAndLayout();
		}

		if (result.ShouldApplyCreateTunnelNodeFilters)
		{
			_applyCreateTunnelNodeFilters();
		}
	}
}
