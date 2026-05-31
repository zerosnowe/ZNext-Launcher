using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ZNext.ViewModels;

namespace ZNext.Services;

internal sealed class CreateTunnelNodeCardMapper
{
	public IReadOnlyList<CreateTunnelNodeCard> Map(IEnumerable<CreateProxyNodeDto> nodes, string currentUserGroup)
	{
		string normalizedUserGroup = (currentUserGroup ?? string.Empty).Trim().ToLowerInvariant();
		bool currentUserIsVip = IsVipUserGroup(normalizedUserGroup);
		List<CreateTunnelNodeCard> cards = new List<CreateTunnelNodeCard>();

		foreach (CreateProxyNodeDto node in nodes ?? Enumerable.Empty<CreateProxyNodeDto>())
		{
			string[] protocols = SplitBySemicolon(node.allowType);
			(int minPort, int maxPort) = ParsePortRange(node.allowPort);
			string[] allowedGroups = SplitBySemicolon(node.allowGroup);
			bool isVipNode = IsVipOnlyNodeByAllowedGroups(allowedGroups);
			bool canUseByGroup = CanUseNodeByGroup(allowedGroups, normalizedUserGroup);
			bool isVipRestricted = isVipNode && !currentUserIsVip;
			int loadPercent = Math.Clamp(node.loadPercent, 0, 100);
			bool canUse = node.isOnline && loadPercent < 90 && canUseByGroup && !isVipRestricted;
			int bandwidthMbps = TryParseBandwidthMbps(node.bandwidth ?? string.Empty, out int parsedMbps)
				? parsedMbps
				: 0;

			cards.Add(new CreateTunnelNodeCard
			{
				NodeId = node.nodeId,
				Name = node.name,
				Description = string.IsNullOrWhiteSpace(node.description) ? "暂无描述" : node.description,
				RegionCode = node.region,
				RegionText = ConvertRegionCodeToText(node.region),
				CountryCategory = ConvertRegionCodeToCountryCategory(node.region),
				BandwidthText = string.IsNullOrWhiteSpace(node.bandwidth) ? "-" : node.bandwidth,
				BandwidthMbps = bandwidthMbps,
				LoadPercent = loadPercent,
				LoadBarBrush = CreateTunnelNodeCard.ResolveLoadBarBrush(loadPercent),
				IsOnline = node.isOnline,
				Protocols = protocols,
				PortMin = minPort,
				PortMax = maxPort,
				AllowedGroups = allowedGroups,
				IsVipNode = isVipNode,
				IsVipRestricted = isVipRestricted,
				CanUse = canUse
			});
		}

		return cards;
	}

	private static bool TryParseBandwidthMbps(string text, out int mbps)
	{
		mbps = 0;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		Match match = Regex.Match(text, @"(\d+)\s*Mbps", RegexOptions.IgnoreCase);
		if (!match.Success)
		{
			match = Regex.Match(text, @"(\d+)");
		}

		return match.Success && int.TryParse(match.Groups[1].Value, out mbps);
	}

	private static string[] SplitBySemicolon(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Array.Empty<string>();
		}

		return value.Split(';', StringSplitOptions.RemoveEmptyEntries)
			.Select(part => part.Trim().ToLowerInvariant())
			.Where(part => !string.IsNullOrWhiteSpace(part))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private static (int MinPort, int MaxPort) ParsePortRange(string allowPort)
	{
		int minPort = 1;
		int maxPort = 65535;
		if (string.IsNullOrWhiteSpace(allowPort))
		{
			return (minPort, maxPort);
		}

		string[] parts = allowPort.Split('-', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length != 2)
		{
			return (minPort, maxPort);
		}

		if (int.TryParse(parts[0].Trim(), out int min))
		{
			minPort = Math.Clamp(min, 1, 65535);
		}
		if (int.TryParse(parts[1].Trim(), out int max))
		{
			maxPort = Math.Clamp(max, minPort, 65535);
		}

		return (minPort, maxPort);
	}

	private static bool CanUseNodeByGroup(string[] allowedGroups, string currentGroup)
	{
		if (allowedGroups.Length == 0)
		{
			return true;
		}

		string lower = currentGroup.Trim().ToLowerInvariant();
		if (lower is "admin" or "sponsor")
		{
			return true;
		}
		if (!string.IsNullOrWhiteSpace(lower))
		{
			return allowedGroups.Contains(lower, StringComparer.OrdinalIgnoreCase);
		}
		return allowedGroups.Contains("default", StringComparer.OrdinalIgnoreCase)
			|| allowedGroups.Contains("norealname", StringComparer.OrdinalIgnoreCase);
	}

	private static bool IsVipOnlyNodeByAllowedGroups(string[] allowedGroups)
	{
		if (allowedGroups.Length == 0)
		{
			return false;
		}

		bool hasVipLike = false;
		foreach (string group in allowedGroups)
		{
			string normalized = (group ?? string.Empty).Trim().ToLowerInvariant();
			bool isVipLike = normalized.Contains("vip", StringComparison.Ordinal)
				|| normalized is "sponsor" or "admin";
			if (!isVipLike)
			{
				return false;
			}

			hasVipLike = true;
		}

		return hasVipLike;
	}

	private static bool IsVipUserGroup(string currentGroup)
	{
		if (string.IsNullOrWhiteSpace(currentGroup))
		{
			return false;
		}

		string normalized = currentGroup.Trim().ToLowerInvariant();
		return normalized is "admin" or "sponsor" || normalized.Contains("vip", StringComparison.Ordinal);
	}

	private static string ConvertRegionCodeToText(string region)
	{
		return region?.Trim().ToLowerInvariant() switch
		{
			"cn" => "中国大陆",
			"cnos" => "港澳台地区",
			"oversea" => "海外",
			_ => string.IsNullOrWhiteSpace(region) ? "-" : region
		};
	}

	private static string ConvertRegionCodeToCountryCategory(string region)
	{
		return region?.Trim().ToLowerInvariant() switch
		{
			"cn" => "中国大陆",
			"cnos" => "港澳台地区",
			"oversea" => "海外",
			_ => "其他"
		};
	}
}
