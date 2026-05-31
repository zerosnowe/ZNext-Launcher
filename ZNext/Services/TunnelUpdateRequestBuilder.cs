using System.Collections.Generic;
using System.Text.Json;

namespace ZNext.Services;

internal sealed class TunnelUpdateRequestBuilder
{
	public TunnelUpdateRequestBuildResult Build(TunnelUpdateRequestInput input)
	{
		string proxyName = input.ProxyName.Trim();
		string localIp = input.LocalIp.Trim();
		string protocol = TunnelProtocolRules.Normalize(input.Protocol);

		if (string.IsNullOrWhiteSpace(proxyName))
		{
			return TunnelUpdateRequestBuildResult.Failed("请填写隧道名称");
		}
		if (string.IsNullOrWhiteSpace(localIp))
		{
			return TunnelUpdateRequestBuildResult.Failed("请填写本地地址");
		}
		if (input.LocalPort is < 1 or > 65535)
		{
			return TunnelUpdateRequestBuildResult.Failed("本地端口必须在 1-65535");
		}

		bool isHttpLike = TunnelProtocolRules.IsHttpLike(protocol);
		if (!isHttpLike && input.RemotePort is < 1 or > 65535)
		{
			return TunnelUpdateRequestBuildResult.Failed("远程端口必须在 1-65535");
		}

		List<string> domains = TunnelDomainParser.Split(input.DomainsText);
		if (isHttpLike && domains.Count == 0)
		{
			return TunnelUpdateRequestBuildResult.Failed("HTTP/HTTPS 类型请至少填写一个域名");
		}

		return TunnelUpdateRequestBuildResult.Created(new TunnelUpdateRequest
		{
			proxyId = input.ProxyId,
			nodeId = input.NodeId,
			proxyName = proxyName,
			localIp = localIp,
			localPort = input.LocalPort,
			remotePort = isHttpLike ? 0 : input.RemotePort,
			domain = isHttpLike ? JsonSerializer.Serialize(domains) : string.Empty,
			location = string.Empty,
			accessKey = string.Empty,
			httpPlugin = TunnelProtocolRules.GetHttpPlugin(protocol),
			hostHeaderRewrite = string.Empty,
			requestHeaders = new Dictionary<string, string>(),
			responseHeaders = new Dictionary<string, string>(),
			httpUser = string.Empty,
			httpPassword = string.Empty,
			crtPath = string.Empty,
			keyPath = string.Empty,
			useEncryption = false,
			useCompression = false,
			proxyProtocolVersion = string.Empty,
			proxyType = protocol,
			transportProtocol = "tcp",
			locations = string.Empty
		});
	}
}
