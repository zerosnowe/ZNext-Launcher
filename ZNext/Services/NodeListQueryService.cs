using System;
using System.Collections.Generic;
using System.Linq;

namespace ZNext.Services;

internal sealed class NodeListQueryService
{
	public IReadOnlyList<NodeInfoWithStatus> ApplyFilters(
		IEnumerable<NodeInfoWithStatus> nodes,
		NodeListFilter filter)
	{
		IEnumerable<NodeInfoWithStatus> source = nodes ?? Enumerable.Empty<NodeInfoWithStatus>();
		string keyword = filter.Keyword.Trim();

		if (keyword.Length > 0)
		{
			source = source.Where(node => MatchesKeyword(node, keyword));
		}
		if (filter.HideOffline)
		{
			source = source.Where(node => node.NodeStatus?.IsOnline ?? false);
		}
		if (filter.HideOverloaded)
		{
			source = source.Where(node => (node.NodeStatus?.LoadPercent ?? 0) <= 90);
		}

		return source.ToList();
	}

	public NodeListStatistics CalculateStatistics(IReadOnlyCollection<NodeInfoWithStatus> nodes)
	{
		int totalNodeCount = nodes.Count;
		int onlineNodeCount = nodes.Count(node => node.NodeStatus?.IsOnline ?? false);
		int onlineUserCount = nodes.Sum(node => node.NodeStatus?.OnlineClient ?? 0);
		int onlineTunnelCount = nodes.Sum(node => node.NodeStatus?.OnlineProxy ?? 0);
		long inboundBytes = nodes.Sum(node => node.NodeStatus?.TotalTrafficIn ?? 0);
		long outboundBytes = nodes.Sum(node => node.NodeStatus?.TotalTrafficOut ?? 0);

		return new NodeListStatistics(
			$"{onlineNodeCount} / {totalNodeCount}",
			onlineUserCount.ToString(),
			onlineTunnelCount.ToString(),
			DisplayFormatter.FormatBytesToGb(inboundBytes),
			DisplayFormatter.FormatBytesToGb(outboundBytes));
	}

	private static bool MatchesKeyword(NodeInfoWithStatus node, string keyword)
	{
		NodeInfo? nodeInfo = node.NodeInfo;
		if (nodeInfo == null)
		{
			return false;
		}

		return (!string.IsNullOrWhiteSpace(nodeInfo.Name)
				&& nodeInfo.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
			|| nodeInfo.NodeId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase);
	}
}
