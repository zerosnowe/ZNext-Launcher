using ZNext.Services;

namespace ZNext.ViewModels;

internal sealed class TunnelsSectionViewModel : ObservableObject
{
	private readonly TunnelListQueryService _queryService;
	private readonly List<TunnelInfo> _tunnels = new();
	private IReadOnlyList<TunnelInfo> _visibleTunnels = Array.Empty<TunnelInfo>();
	private string _searchKeyword = string.Empty;
	private bool _isLoading;
	private bool _hasError;
	private bool _hasVisibleTunnels;
	private bool _isEmpty;
	private bool _isListView;

	public TunnelsSectionViewModel(TunnelListQueryService queryService)
	{
		_queryService = queryService;
	}

	public IReadOnlyList<TunnelInfo> Tunnels => _tunnels;

	public IReadOnlyList<TunnelInfo> VisibleTunnels
	{
		get => _visibleTunnels;
		private set => SetProperty(ref _visibleTunnels, value);
	}

	public string SearchKeyword
	{
		get => _searchKeyword;
		set
		{
			if (SetProperty(ref _searchKeyword, value ?? string.Empty))
			{
				ApplyFilters();
			}
		}
	}

	public bool IsLoading
	{
		get => _isLoading;
		private set => SetProperty(ref _isLoading, value);
	}

	public bool HasError
	{
		get => _hasError;
		private set => SetProperty(ref _hasError, value);
	}

	public bool HasVisibleTunnels
	{
		get => _hasVisibleTunnels;
		private set => SetProperty(ref _hasVisibleTunnels, value);
	}

	public bool IsEmpty
	{
		get => _isEmpty;
		private set => SetProperty(ref _isEmpty, value);
	}

	public bool IsListView
	{
		get => _isListView;
		set => SetProperty(ref _isListView, value);
	}

	public bool HasCachedTunnels => _tunnels.Count > 0;

	public bool HasVisibleData => HasVisibleTunnels && _tunnels.Count > 0;

	public void Clear()
	{
		_tunnels.Clear();
		VisibleTunnels = Array.Empty<TunnelInfo>();
		HasVisibleTunnels = false;
		IsEmpty = false;
		HasError = false;
		IsLoading = false;
	}

	public void BeginLoading(bool keepVisibleData)
	{
		IsLoading = true;
		HasError = false;
		IsEmpty = false;
		if (!keepVisibleData)
		{
			VisibleTunnels = Array.Empty<TunnelInfo>();
			HasVisibleTunnels = false;
		}
	}

	public void ShowSignedOutError()
	{
		IsLoading = false;
		HasError = true;
		VisibleTunnels = Array.Empty<TunnelInfo>();
		HasVisibleTunnels = false;
		IsEmpty = false;
	}

	public void ShowLoadError(bool keepVisibleData)
	{
		IsLoading = false;
		HasError = true;
		IsEmpty = false;
		if (!keepVisibleData)
		{
			VisibleTunnels = Array.Empty<TunnelInfo>();
			HasVisibleTunnels = false;
		}
	}

	public void SetTunnels(IEnumerable<TunnelInfo> tunnels)
	{
		_tunnels.Clear();
		_tunnels.AddRange(tunnels ?? Enumerable.Empty<TunnelInfo>());
		IsLoading = false;
		HasError = false;
		ApplyFilters();
	}

	public void FinishCachedRefresh()
	{
		IsLoading = false;
		HasError = false;
		ApplyFilters();
	}

	public IReadOnlyList<TunnelInfo> ApplyFilters()
	{
		IReadOnlyList<TunnelInfo> filtered = _queryService.ApplyFilters(_tunnels, new TunnelListFilter(SearchKeyword.Trim()));
		VisibleTunnels = filtered;
		HasVisibleTunnels = filtered.Count > 0;
		IsEmpty = !HasError && !IsLoading && filtered.Count == 0;
		return filtered;
	}
}
