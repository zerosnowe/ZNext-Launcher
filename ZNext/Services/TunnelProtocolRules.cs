namespace ZNext.Services;

internal static class TunnelProtocolRules
{
	public static string Normalize(string protocol)
	{
		return (protocol ?? string.Empty).Trim().ToLowerInvariant();
	}

	public static bool IsHttpLike(string protocol)
	{
		return Normalize(protocol) is "http" or "https";
	}

	public static string GetHttpPlugin(string protocol)
	{
		return Normalize(protocol) switch
		{
			"http" => "http2http",
			"https" => "https2https",
			_ => string.Empty
		};
	}
}
