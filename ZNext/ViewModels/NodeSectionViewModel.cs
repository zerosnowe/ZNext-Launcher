using ZNext.Services;

namespace ZNext.ViewModels;

internal sealed class NodeSectionViewModel : ObservableObject
{
	private readonly NodeListQueryService _queryService;
	private readonly List<NodeInfoWithStatus> _nodes = new();
	private IReadOnlyList<NodeInfoWithStatus> _visibleNodes = Array.Empty<NodeInfoWithStatus>();
	private string _searchKeyword = string.Empty;
	private bool _hideOffline;
	private bool _hideOverloaded;
	private bool _isLoading;
	private bool _hasError;
	private bool _hasVisibleNodes;
	private bool _isEmpty;
	private string _onlineNodesText = "0 / 0";
	private string _onlineUsersText = "0";
	private string _onlineTunnelsText = "0";
	private string _todayInText = "0.00 GB";
	private string _todayOutText = "0.00 GB";

	public NodeSectionViewModel(NodeListQueryService queryService)
	{
		_queryService = queryService;
	}

	public IReadOnlyList<NodeInfoWithStatus> Nodes => _nodes;

	public IReadOnlyList<NodeInfoWithStatus> VisibleNodes
	{
		get => _visibleNodes;
		private set => SetProperty(ref _visibleNodes, value);
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

	public bool HideOffline
	{
		get => _hideOffline;
		set
		{
			if (SetProperty(ref _hideOffline, value))
			{
				ApplyFilters();
			}
		}
	}

	public bool HideOverloaded
	{
		get => _hideOverloaded;
		set
		{
			if (SetProperty(ref _hideOverloaded, value))
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

	public bool HasVisibleNodes
	{
		get => _hasVisibleNodes;
		private set => SetProperty(ref _hasVisibleNodes, value);
	}

	public bool IsEmpty
	{
		get => _isEmpty;
		private set => SetProperty(ref _isEmpty, value);
	}

	public string OnlineNodesText
	{
		get => _onlineNodesText;
		private set => SetProperty(ref _onlineNodesText, value);
	}

	public string OnlineUsersText
	{
		get => _onlineUsersText;
		private set => SetProperty(ref _onlineUsersText, value);
	}

	public string OnlineTunnelsText
	{
		get => _onlineTunnelsText;
		private set => SetProperty(ref _onlineTunnelsText, value);
	}

	public string TodayInText
	{
		get => _todayInText;
		private set => SetProperty(ref _todayInText, value);
	}

	public string TodayOutText
	{
		get => _todayOutText;
		private set => SetProperty(ref _todayOutText, value);
	}

	public void BeginLoading()
	{
		IsLoading = true;
		HasError = false;
		IsEmpty = false;
		VisibleNodes = Array.Empty<NodeInfoWithStatus>();
		HasVisibleNodes = false;
	}

	public void ShowSignedOutError()
	{
		Clear();
		HasError = true;
	}

	public void ShowLoadError()
	{
		Clear();
		HasError = true;
	}

	public void SetNodes(IEnumerable<NodeInfoWithStatus> nodes)
	{
		_nodes.Clear();
		_nodes.AddRange(nodes ?? Enumerable.Empty<NodeInfoWithStatus>());
		IsLoading = false;
		HasError = false;
		UpdateStatistics();
		ApplyFilters();
	}

	public void Clear()
	{
		_nodes.Clear();
		VisibleNodes = Array.Empty<NodeInfoWithStatus>();
		HasVisibleNodes = false;
		IsEmpty = false;
		HasError = false;
		IsLoading = false;
		ApplyStatistics(new NodeListStatistics("0 / 0", "0", "0", "0.00 GB", "0.00 GB"));
	}

	public IReadOnlyList<NodeInfoWithStatus> ApplyFilters()
	{
		IReadOnlyList<NodeInfoWithStatus> filtered = _queryService.ApplyFilters(_nodes, new NodeListFilter(
			SearchKeyword.Trim(),
			HideOffline,
			HideOverloaded));
		VisibleNodes = filtered;
		HasVisibleNodes = filtered.Count > 0;
		IsEmpty = !HasError && !IsLoading && filtered.Count == 0;
		return filtered;
	}

	private void UpdateStatistics()
	{
		ApplyStatistics(_queryService.CalculateStatistics(_nodes));
	}

	private void ApplyStatistics(NodeListStatistics statistics)
	{
		OnlineNodesText = statistics.OnlineNodesText;
		OnlineUsersText = statistics.OnlineUsersText;
		OnlineTunnelsText = statistics.OnlineTunnelsText;
		TodayInText = statistics.TodayInText;
		TodayOutText = statistics.TodayOutText;
	}
}
