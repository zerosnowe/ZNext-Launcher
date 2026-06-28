using System;
using System.Collections.Generic;
using System.Text.Json;
using ZNext.Infrastructure.Serialization;

namespace ZNext.Services;

internal sealed class CreateTunnelRequestBuilder
{
	public CreateTunnelRequestBuildResult Build(CreateTunnelRequestInput input)
	{
		string proxyName = input.ProxyName.Trim();
		string localIp = input.LocalIp.Trim();
		string protocol = TunnelProtocolRules.Normalize(input.Protocol);
		string transportProtocol = NormalizeTransportProtocol(input.TransportProtocol);
		string proxyProtocolVersion = NormalizeProxyProtocolVersion(input.ProxyProtocolVersion);

		if (string.IsNullOrWhiteSpace(proxyName))
		{
			return CreateTunnelRequestBuildResult.Failed("请填写隧道名称");
		}
		if (string.IsNullOrWhiteSpace(localIp))
		{
			return CreateTunnelRequestBuildResult.Failed("请填写本地地址");
		}
		if (input.LocalPort is < 1 or > 65535)
		{
			return CreateTunnelRequestBuildResult.Failed("本地端口必须在 1-65535");
		}
		if (string.IsNullOrWhiteSpace(protocol))
		{
			return CreateTunnelRequestBuildResult.Failed("请选择协议类型");
		}

		bool isHttpLike = TunnelProtocolRules.IsHttpLike(protocol);
		if (!isHttpLike && (input.RemotePort < input.PortMin || input.RemotePort > input.PortMax))
		{
			return CreateTunnelRequestBuildResult.Failed($"远程端口需在 {input.PortMin}-{input.PortMax}");
		}

		List<string> domains = TunnelDomainParser.Split(input.DomainsText);
		if (isHttpLike && domains.Count == 0)
		{
			return CreateTunnelRequestBuildResult.Failed("HTTP/HTTPS 类型请至少填写一个域名");
		}

		return CreateTunnelRequestBuildResult.Created(new CreateProxyRequest
		{
			nodeId = input.NodeId,
			proxyName = proxyName,
			localIp = localIp,
			localPort = input.LocalPort,
			remotePort = isHttpLike ? 0 : input.RemotePort,
			domain = isHttpLike ? JsonSerializer.Serialize(domains, AppJsonSerializerContext.Default.ListString) : string.Empty,
			proxyType = protocol,
			httpPlugin = TunnelProtocolRules.GetHttpPlugin(protocol),
			crtPath = protocol == "https" ? input.CertificatePath.Trim() : string.Empty,
			keyPath = protocol == "https" ? input.KeyPath.Trim() : string.Empty,
			transportProtocol = transportProtocol,
			proxyProtocolVersion = proxyProtocolVersion,
			useEncryption = input.UseEncryption,
			useCompression = input.UseCompression,
			requestHeaders = new Dictionary<string, string>(),
			responseHeaders = new Dictionary<string, string>()
		});
	}

	private static string NormalizeTransportProtocol(string value)
	{
		string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
		return normalized == "quic" ? "quic" : "tcp";
	}

	private static string NormalizeProxyProtocolVersion(string value)
	{
		string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
		return normalized is "v1" or "v2" ? normalized : string.Empty;
	}
}
