using System;
using System.Collections.Generic;
using System.Linq;

namespace ZNext.Services;

internal static class TunnelDomainParser
{
	public static List<string> Split(string domainsText)
	{
		return (domainsText ?? string.Empty)
			.Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries)
			.Select(domain => domain.Trim())
			.Where(domain => !string.IsNullOrWhiteSpace(domain))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}
}
