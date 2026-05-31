using System.Diagnostics;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class PageDataLoadCoordinator
{
	private readonly PageLoadStateCoordinator _loadState;
	private readonly UserSessionService _userSessionService;
	private readonly NodeService _nodeService;
	private readonly TunnelService _tunnelService;
	private readonly CreateProxyService _createProxyService;
	private readonly UserProfileSettingsService _userProfileSettingsService;
	private readonly CreateTunnelNodeCardMapper _createTunnelNodeCardMapper;
	private readonly NodeSectionViewModel _nodesViewModel;
	private readonly TunnelsSectionViewModel _tunnelsViewModel;
	private readonly CreateTunnelSectionViewModel _createTunnelViewModel;

	public PageDataLoadCoordinator(
		PageLoadStateCoordinator loadState,
		UserSessionService userSessionService,
		NodeService nodeService,
		TunnelService tunnelService,
		CreateProxyService createProxyService,
		UserProfileSettingsService userProfileSettingsService,
		CreateTunnelNodeCardMapper createTunnelNodeCardMapper,
		NodeSectionViewModel nodesViewModel,
		TunnelsSectionViewModel tunnelsViewModel,
		CreateTunnelSectionViewModel createTunnelViewModel)
	{
		_loadState = loadState;
		_userSessionService = userSessionService;
		_nodeService = nodeService;
		_tunnelService = tunnelService;
		_createProxyService = createProxyService;
		_userProfileSettingsService = userProfileSettingsService;
		_createTunnelNodeCardMapper = createTunnelNodeCardMapper;
		_nodesViewModel = nodesViewModel;
		_tunnelsViewModel = tunnelsViewModel;
		_createTunnelViewModel = createTunnelViewModel;
	}

	public async Task LoadNodesAsync()
	{
		try
		{
			_loadState.MarkNodesLoading();
			_nodesViewModel.BeginLoading();
			if (!_userSessionService.IsSignedIn)
			{
				_loadState.MarkNodesNotLoaded();
				_nodesViewModel.ShowSignedOutError();
				return;
			}

			_userSessionService.SynchronizeTokens();
			List<NodeInfoWithStatus> nodesWithStatus = await _nodeService.GetNodesWithStatusAsync();
			_nodesViewModel.SetNodes(nodesWithStatus);
			_loadState.MarkNodesLoaded();
		}
		catch (Exception ex)
		{
			Debug.WriteLine("LoadNodesAsync failed: " + ex.Message);
			_nodesViewModel.ShowLoadError();
			_loadState.MarkNodesNotLoaded();
		}
	}

	public async Task<PageDataLoadUiResult> LoadTunnelsAsync(
		bool forceReload,
		bool hasVisibleData,
		Action<IEnumerable<TunnelInfo>> syncTunnelLocalRunStates)
	{
		if (!_loadState.TryBeginTunnelLoad())
		{
			return PageDataLoadUiResult.None;
		}

		try
		{
			if (!_userSessionService.IsSignedIn)
			{
				_tunnelsViewModel.ShowSignedOutError();
				_loadState.MarkTunnelsNotLoaded();
				return PageDataLoadUiResult.RebuildAutoStartChecklist;
			}

			if (_loadState.CanUseLoadedTunnels(forceReload, _tunnelsViewModel.HasCachedTunnels))
			{
				return PageDataLoadUiResult.None;
			}

			if (_loadState.CanUseRecentTunnelCache(forceReload, _tunnelsViewModel.HasCachedTunnels))
			{
				syncTunnelLocalRunStates(_tunnelsViewModel.Tunnels);
				_tunnelsViewModel.FinishCachedRefresh();
				_loadState.MarkTunnelsLoaded();
				return PageDataLoadUiResult.ApplyTunnelFiltersAndLayout;
			}

			_loadState.MarkTunnelsLoading();
			_tunnelsViewModel.BeginLoading(hasVisibleData);
			_userSessionService.SynchronizeTokens();
			TunnelListResult tunnelResult = await _tunnelService.GetTunnelsAsync();
			if (tunnelResult.Success && tunnelResult.Tunnels != null)
			{
				_loadState.MarkTunnelsLoaded();
				_tunnelsViewModel.SetTunnels(tunnelResult.Tunnels);
				syncTunnelLocalRunStates(_tunnelsViewModel.Tunnels);
				return new PageDataLoadUiResult(
					ShouldRebuildAutoStartChecklist: true,
					ShouldApplyTunnelFiltersAndLayout: tunnelResult.Tunnels.Count > 0,
					ShouldApplyCreateTunnelNodeFilters: false);
			}

			_loadState.MarkTunnelsNotLoaded();
			_tunnelsViewModel.ShowLoadError(hasVisibleData);
			return PageDataLoadUiResult.RebuildAutoStartChecklist;
		}
		catch (Exception ex)
		{
			Debug.WriteLine("LoadTunnelsAsync failed: " + ex.Message);
			_loadState.MarkTunnelsNotLoaded();
			_tunnelsViewModel.ShowLoadError(hasVisibleData);
			return PageDataLoadUiResult.RebuildAutoStartChecklist;
		}
		finally
		{
			_loadState.EndTunnelLoad();
		}
	}

	public async Task<PageDataLoadUiResult> LoadCreateTunnelNodesAsync(bool forceReload)
	{
		try
		{
			if (!_loadState.ShouldLoadCreateTunnelNodes(forceReload))
			{
				return PageDataLoadUiResult.ApplyCreateTunnelNodeFilters;
			}

			_createTunnelViewModel.BeginLoading();
			if (!_userSessionService.IsSignedIn)
			{
				_loadState.MarkCreateTunnelNodesNotLoaded();
				_createTunnelViewModel.ShowError();
				return PageDataLoadUiResult.None;
			}

			_userSessionService.SynchronizeTokens();
			CreateProxyDataResult result = await _createProxyService.GetCreateProxyDataAsync();
			if (!result.Success || result.Data == null)
			{
				_loadState.MarkCreateTunnelNodesNotLoaded();
				_createTunnelViewModel.ShowError();
				return PageDataLoadUiResult.None;
			}

			string normalizedGroup = ((!string.IsNullOrWhiteSpace(result.Data.currentGroup)
					? result.Data.currentGroup
					: _userProfileSettingsService.LoadUserGroup()) ?? string.Empty)
				.Trim()
				.ToLowerInvariant();
			_createTunnelViewModel.SetNodes(_createTunnelNodeCardMapper.Map(result.Data.nodes, normalizedGroup));
			_loadState.MarkCreateTunnelNodesLoaded();
			return PageDataLoadUiResult.ApplyCreateTunnelNodeFilters;
		}
		catch
		{
			_createTunnelViewModel.ShowError();
			_loadState.MarkCreateTunnelNodesNotLoaded();
			return PageDataLoadUiResult.None;
		}
	}
}

internal sealed record PageDataLoadUiResult(
	bool ShouldRebuildAutoStartChecklist,
	bool ShouldApplyTunnelFiltersAndLayout,
	bool ShouldApplyCreateTunnelNodeFilters)
{
	public static readonly PageDataLoadUiResult None = new(false, false, false);
	public static readonly PageDataLoadUiResult RebuildAutoStartChecklist = new(true, false, false);
	public static readonly PageDataLoadUiResult ApplyTunnelFiltersAndLayout = new(false, true, false);
	public static readonly PageDataLoadUiResult ApplyCreateTunnelNodeFilters = new(false, false, true);
}
