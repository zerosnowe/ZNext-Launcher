using System;
using System.Collections.Generic;
using System.Linq;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class CreateTunnelNodeQueryService
{
	private const string AllCountriesLabel = "全部国家/地区";

	public IReadOnlyList<CreateTunnelNodeCard> ApplyFilters(
		IEnumerable<CreateTunnelNodeCard> nodes,
		CreateTunnelNodeFilter filter)
	{
		IEnumerable<CreateTunnelNodeCard> source = nodes ?? Enumerable.Empty<CreateTunnelNodeCard>();
		string country = filter.Country.Trim();
		string keyword = filter.Keyword.Trim();

		if (country.Length > 0 && !string.Equals(country, AllCountriesLabel, StringComparison.Ordinal))
		{
			source = source.Where(node => string.Equals(node.CountryCategory, country, StringComparison.OrdinalIgnoreCase));
		}
		if (keyword.Length > 0)
		{
			source = source.Where(node => MatchesKeyword(node, keyword));
		}
		if (filter.CanWeb)
		{
			source = source.Where(IsWebCapable);
		}
		if (filter.HighBandwidth)
		{
			source = source.Where(node => node.BandwidthMbps >= 100);
		}
		if (filter.NotOverloaded)
		{
			source = source.Where(node => node.LoadPercent < 90);
		}
		if (!filter.IncludeNoPermission)
		{
			source = source.Where(node => node.CanUse);
		}

		return source
			.OrderByDescending(node => node.CanUse)
			.ThenBy(node => node.NodeId)
			.ToList();
	}

	private static bool MatchesKeyword(CreateTunnelNodeCard node, string keyword)
	{
		return node.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
			|| node.RegionText.Contains(keyword, StringComparison.OrdinalIgnoreCase)
			|| node.NodeId.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsWebCapable(CreateTunnelNodeCard node)
	{
		return node.Protocols.Any(protocol =>
			string.Equals(protocol, "http", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(protocol, "https", StringComparison.OrdinalIgnoreCase));
	}
}
