using System;
using System.Collections.Generic;
using System.Linq;

namespace ZNext.Services;

internal sealed class TunnelListQueryService
{
	public IReadOnlyList<TunnelInfo> ApplyFilters(
		IEnumerable<TunnelInfo> tunnels,
		TunnelListFilter filter)
	{
		IEnumerable<TunnelInfo> source = tunnels ?? Enumerable.Empty<TunnelInfo>();
		string keyword = filter.Keyword.Trim();
		if (keyword.Length == 0)
		{
			return source.ToList();
		}

		return source
			.Where(tunnel => MatchesKeyword(tunnel, keyword))
			.ToList();
	}

	private static bool MatchesKeyword(TunnelInfo tunnel, string keyword)
	{
		return (tunnel.Name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
			|| tunnel.IdDisplayText.Contains(keyword, StringComparison.OrdinalIgnoreCase)
			|| tunnel.Id.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
			|| (tunnel.Type?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
			|| tunnel.remotePort.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase)
			|| (tunnel.NodeDisplayText?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false);
	}
}
