namespace ZNext.Services;

internal sealed record CreateTunnelNodeFilter(
	string Keyword,
	string Country,
	bool CanWeb,
	bool HighBandwidth,
	bool NotOverloaded,
	bool IncludeNoPermission);
