namespace ZNext.Services;

internal sealed class PageLoadStateCoordinator
{
	private static readonly TimeSpan TunnelListCacheDuration = TimeSpan.FromSeconds(20.0);
	private DateTimeOffset _lastTunnelLoadAt = DateTimeOffset.MinValue;

	public bool HasLoadedNodes { get; private set; }
	public bool HasLoadedTunnels { get; private set; }
	public bool HasLoadedCreateTunnelNodes { get; private set; }
	public bool IsLoadingTunnels { get; private set; }

	public bool ShouldLoadNodes()
	{
		return !HasLoadedNodes;
	}

	public void MarkNodesLoading()
	{
		HasLoadedNodes = false;
	}

	public void MarkNodesLoaded()
	{
		HasLoadedNodes = true;
	}

	public void MarkNodesNotLoaded()
	{
		HasLoadedNodes = false;
	}

	public bool ShouldLoadTunnels()
	{
		return !HasLoadedTunnels;
	}

	public bool TryBeginTunnelLoad()
	{
		if (IsLoadingTunnels)
		{
			return false;
		}

		IsLoadingTunnels = true;
		return true;
	}

	public void EndTunnelLoad()
	{
		IsLoadingTunnels = false;
	}

	public bool CanUseLoadedTunnels(bool forceReload, bool hasCachedTunnels)
	{
		return !forceReload && HasLoadedTunnels && hasCachedTunnels;
	}

	public bool CanUseRecentTunnelCache(bool forceReload, bool hasCachedTunnels)
	{
		return !forceReload
			&& hasCachedTunnels
			&& DateTimeOffset.Now - _lastTunnelLoadAt < TunnelListCacheDuration;
	}

	public void MarkTunnelsLoading()
	{
		HasLoadedTunnels = false;
	}

	public void MarkTunnelsLoaded()
	{
		HasLoadedTunnels = true;
		_lastTunnelLoadAt = DateTimeOffset.Now;
	}

	public void MarkTunnelsNotLoaded()
	{
		HasLoadedTunnels = false;
	}

	public void ResetTunnels()
	{
		HasLoadedTunnels = false;
		_lastTunnelLoadAt = DateTimeOffset.MinValue;
	}

	public bool ShouldLoadCreateTunnelNodes(bool forceReload)
	{
		return forceReload || !HasLoadedCreateTunnelNodes;
	}

	public void MarkCreateTunnelNodesLoaded()
	{
		HasLoadedCreateTunnelNodes = true;
	}

	public void MarkCreateTunnelNodesNotLoaded()
	{
		HasLoadedCreateTunnelNodes = false;
	}

	public void ResetAll()
	{
		HasLoadedNodes = false;
		HasLoadedTunnels = false;
		HasLoadedCreateTunnelNodes = false;
		_lastTunnelLoadAt = DateTimeOffset.MinValue;
	}
}
