using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ZNext.Services;

internal static class TunnelDetailsFormatter
{
	public static string Format(TunnelInfo tunnel, string? resolvedLink)
	{
		return $"隧道 ID: {tunnel.Id}\n"
			+ $"名称: {tunnel.Name}\n"
			+ $"协议: {tunnel.Type}\n"
			+ $"本地地址: {tunnel.LocalAddr}\n"
			+ $"远程端口: {tunnel.remotePort}\n"
			+ $"域名: {ResolveDomainText(tunnel, resolvedLink)}\n"
			+ $"节点: {tunnel.NodeDisplayText}\n"
			+ $"状态: {tunnel.OnlineStatusText}";
	}

	private static string ResolveDomainText(TunnelInfo tunnel, string? resolvedLink)
	{
		if (!string.IsNullOrWhiteSpace(resolvedLink))
		{
			return resolvedLink;
		}

		string[] domains = ParseDomains(tunnel.domain);
		if (domains.Length > 0)
		{
			return string.Join(", ", domains);
		}

		return string.IsNullOrWhiteSpace(tunnel.domain) ? "-" : tunnel.domain;
	}

	public static string[] ParseDomains(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return Array.Empty<string>();
		}

		string text = value.Trim();
		var result = new List<string>();
		if (!TryCollectTunnelDomainsFromJson(text, result, 0))
		{
			AddTunnelDomainSegments(text, result);
		}

		return result
			.Select(domain => domain.Trim())
			.Where(domain => !string.IsNullOrWhiteSpace(domain))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToArray();
	}

	private static bool TryCollectTunnelDomainsFromJson(string text, List<string> result, int depth)
	{
		if (depth > 2 || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		try
		{
			using var doc = JsonDocument.Parse(text);
			CollectTunnelDomainsFromElement(doc.RootElement, result, depth);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void CollectTunnelDomainsFromElement(JsonElement element, List<string> result, int depth)
	{
		switch (element.ValueKind)
		{
			case JsonValueKind.String:
				CollectStringValue(element.GetString(), result, depth);
				return;
			case JsonValueKind.Array:
				foreach (JsonElement child in element.EnumerateArray())
				{
					CollectTunnelDomainsFromElement(child, result, depth);
				}
				return;
			case JsonValueKind.Object:
				foreach (JsonProperty property in element.EnumerateObject())
				{
					if (property.NameEquals("domain") ||
						property.NameEquals("domains") ||
						property.NameEquals("host") ||
						property.NameEquals("hostname") ||
						property.NameEquals("url") ||
						property.NameEquals("value") ||
						property.NameEquals("name"))
					{
						CollectTunnelDomainsFromElement(property.Value, result, depth);
					}
				}
				return;
		}
	}

	private static void CollectStringValue(string? value, List<string> result, int depth)
	{
		string item = value?.Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(item))
		{
			return;
		}

		if (LooksLikeTunnelJsonValue(item) && TryCollectTunnelDomainsFromJson(item, result, depth + 1))
		{
			return;
		}

		AddTunnelDomainSegments(item, result);
	}

	private static bool LooksLikeTunnelJsonValue(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		char first = text[0];
		return first == '[' || first == '{' || first == '"';
	}

	private static void AddTunnelDomainSegments(string text, List<string> result)
	{
		foreach (string item in text.Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string normalized = NormalizeTunnelDomainSegment(item);
			if (!string.IsNullOrWhiteSpace(normalized))
			{
				result.Add(normalized);
			}
		}
	}

	private static string NormalizeTunnelDomainSegment(string segment)
	{
		string text = (segment ?? string.Empty).Trim().Trim('"', '\'');
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		if (Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) && !string.IsNullOrWhiteSpace(uri.Host))
		{
			return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
		}

		int slashIndex = text.IndexOf('/');
		if (slashIndex >= 0)
		{
			text = text.Substring(0, slashIndex);
		}

		return text.Trim();
	}
}
