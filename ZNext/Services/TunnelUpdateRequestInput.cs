namespace ZNext.Services;

internal sealed record TunnelUpdateRequestInput(
	int ProxyId,
	int NodeId,
	string ProxyName,
	string LocalIp,
	int LocalPort,
	int RemotePort,
	string Protocol,
	string DomainsText);
