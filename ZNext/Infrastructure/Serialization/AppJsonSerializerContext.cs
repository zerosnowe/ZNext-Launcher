using System.Text.Json;
using System.Text.Json.Serialization;
using ZNext.Services;

namespace ZNext.Infrastructure.Serialization;

[JsonSourceGenerationOptions(
	PropertyNameCaseInsensitive = true,
	WriteIndented = false,
	DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(ApiResponse))]
[JsonSerializable(typeof(AuthLoginRequest))]
[JsonSerializable(typeof(CaptchaTokenRequest))]
[JsonSerializable(typeof(CommonApiResponse))]
[JsonSerializable(typeof(UserInfoCacheData))]
[JsonSerializable(typeof(UserInfoApiResponse))]
[JsonSerializable(typeof(NodeApiResponse))]
[JsonSerializable(typeof(StatusApiResponse))]
[JsonSerializable(typeof(SystemStatusApiResponse))]
[JsonSerializable(typeof(CreateProxyDataDto))]
[JsonSerializable(typeof(FreeNodePortRequest))]
[JsonSerializable(typeof(CreateProxyRequest))]
[JsonSerializable(typeof(ProxyIdRequest))]
[JsonSerializable(typeof(ProxyToggleRequest))]
[JsonSerializable(typeof(TunnelUpdateRequest))]
[JsonSerializable(typeof(List<TunnelNodeInfo>))]
[JsonSerializable(typeof(List<TunnelInfo>))]
[JsonSerializable(typeof(LauncherUpdateListRequest))]
[JsonSerializable(typeof(LauncherUpdateGetRequest))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext
{
}

internal sealed class AuthLoginRequest
{
	public string username { get; set; } = string.Empty;
	public string password { get; set; } = string.Empty;
	public string captchaToken { get; set; } = string.Empty;
}

internal sealed class CaptchaTokenRequest
{
	public string captchaToken { get; set; } = string.Empty;
}

internal sealed class FreeNodePortRequest
{
	public int nodeId { get; set; }
	public string protocol { get; set; } = string.Empty;
}

internal sealed class ProxyIdRequest
{
	public int proxyId { get; set; }
}

internal sealed class ProxyToggleRequest
{
	public int proxyId { get; set; }
	public bool isDisabled { get; set; }
}

internal sealed class LauncherUpdateListRequest
{
	public string path { get; set; } = string.Empty;
	public string password { get; set; } = string.Empty;
	public int page { get; set; }
	public int per_page { get; set; }
	public bool refresh { get; set; }
}

internal sealed class LauncherUpdateGetRequest
{
	public string path { get; set; } = string.Empty;
	public string password { get; set; } = string.Empty;
}
