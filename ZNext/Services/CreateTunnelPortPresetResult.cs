namespace ZNext.Services;

internal sealed record CreateTunnelPortPresetResult(
	int? Port,
	string? PreferredProtocol);
