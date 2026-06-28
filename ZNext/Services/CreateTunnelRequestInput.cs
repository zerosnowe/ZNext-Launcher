namespace ZNext.Services;

internal sealed record CreateTunnelRequestInput(
	int NodeId,
	int PortMin,
	int PortMax,
	string ProxyName,
	string LocalIp,
	int LocalPort,
	int RemotePort,
	string Protocol,
	string DomainsText,
	string CertificatePath,
	string KeyPath,
	string TransportProtocol,
	string ProxyProtocolVersion,
	bool UseEncryption,
	bool UseCompression);
