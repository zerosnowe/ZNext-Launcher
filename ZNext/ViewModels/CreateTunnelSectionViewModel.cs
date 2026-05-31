using ZNext.Services;

namespace ZNext.ViewModels;

internal sealed class CreateTunnelSectionViewModel : ObservableObject
{
	private readonly CreateTunnelNodeQueryService _queryService;
	private readonly List<CreateTunnelNodeCard> _nodes = new();
	private IReadOnlyList<CreateTunnelNodeCard> _visibleNodes = Array.Empty<CreateTunnelNodeCard>();
	private string _searchKeyword = string.Empty;
	private string _country = string.Empty;
	private bool _canWeb;
	private bool _highBandwidth;
	private bool _notOverloaded;
	private bool _includeNoPermission = true;
	private bool _isLoading;
	private bool _hasError;
	private bool _hasVisibleNodes;
	private bool _isEmpty;

	public CreateTunnelSectionViewModel(CreateTunnelNodeQueryService queryService)
	{
		_queryService = queryService;
	}

	public IReadOnlyList<CreateTunnelNodeCard> Nodes => _nodes;

	public IReadOnlyList<CreateTunnelNodeCard> VisibleNodes
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

	public string Country
	{
		get => _country;
		set
		{
			if (SetProperty(ref _country, value ?? string.Empty))
			{
				ApplyFilters();
			}
		}
	}

	public bool CanWeb
	{
		get => _canWeb;
		set
		{
			if (SetProperty(ref _canWeb, value))
			{
				ApplyFilters();
			}
		}
	}

	public bool HighBandwidth
	{
		get => _highBandwidth;
		set
		{
			if (SetProperty(ref _highBandwidth, value))
			{
				ApplyFilters();
			}
		}
	}

	public bool NotOverloaded
	{
		get => _notOverloaded;
		set
		{
			if (SetProperty(ref _notOverloaded, value))
			{
				ApplyFilters();
			}
		}
	}

	public bool IncludeNoPermission
	{
		get => _includeNoPermission;
		set
		{
			if (SetProperty(ref _includeNoPermission, value))
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

	public bool HasNodes => _nodes.Count > 0;

	public void Clear()
	{
		_nodes.Clear();
		VisibleNodes = Array.Empty<CreateTunnelNodeCard>();
		HasVisibleNodes = false;
		IsEmpty = false;
		HasError = false;
		IsLoading = false;
	}

	public void BeginLoading()
	{
		IsLoading = true;
		HasError = false;
		IsEmpty = false;
		VisibleNodes = Array.Empty<CreateTunnelNodeCard>();
		HasVisibleNodes = false;
	}

	public void ShowError()
	{
		IsLoading = false;
		HasError = true;
		IsEmpty = false;
		VisibleNodes = Array.Empty<CreateTunnelNodeCard>();
		HasVisibleNodes = false;
	}

	public void SetNodes(IEnumerable<CreateTunnelNodeCard> nodes)
	{
		_nodes.Clear();
		_nodes.AddRange(nodes ?? Enumerable.Empty<CreateTunnelNodeCard>());
		IsLoading = false;
		HasError = false;
		ApplyFilters();
	}

	public IReadOnlyList<CreateTunnelNodeCard> ApplyFilters()
	{
		IReadOnlyList<CreateTunnelNodeCard> filtered = _queryService.ApplyFilters(_nodes, new CreateTunnelNodeFilter(
			SearchKeyword.Trim(),
			Country.Trim(),
			CanWeb,
			HighBandwidth,
			NotOverloaded,
			IncludeNoPermission));
		VisibleNodes = filtered;
		HasVisibleNodes = filtered.Count > 0;
		IsEmpty = !HasError && !IsLoading && filtered.Count == 0;
		return filtered;
	}
}
