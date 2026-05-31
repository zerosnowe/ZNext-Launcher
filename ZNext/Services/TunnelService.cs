using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ZNext.Services
{
    public class TunnelService
    {
        private const string ApiRootUrl = "https://api.mefrp.com";
        private const string ApiBaseUrl = ApiRootUrl + "/api";
        private const string ProxyListApiUrl = ApiBaseUrl + "/auth/proxy/list";
        private const string UserFrpTokenApiUrl = ApiBaseUrl + "/auth/user/frpToken";
        private const string NodeNameListApiUrl = ApiBaseUrl + "/auth/node/nameList";
        private const string ProxyUpdateApiUrl = ApiBaseUrl + "/auth/proxy/update";
        private const string ProxyKickApiUrl = ApiBaseUrl + "/auth/proxy/kick";
        private const string ProxyToggleApiUrl = ApiBaseUrl + "/auth/proxy/toggle";
        private const string ProxyDeleteApiUrl = ApiBaseUrl + "/auth/proxy/delete";
        private static readonly TimeSpan NodeAddressCacheDuration = TimeSpan.FromMinutes(5.0);

        private readonly IHttpService _httpService;
        private string? _token;
        private Dictionary<int, string>? _nodeAddressCache;
        private DateTimeOffset _nodeAddressCacheTime = DateTimeOffset.MinValue;

        public TunnelService() : this(new HttpService())
        {
        }

        public TunnelService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpService.SetAuthToken(token);
        }

        public void ClearToken()
        {
            _token = null;
            _httpService.ClearAuthToken();
            _nodeAddressCache = null;
            _nodeAddressCacheTime = DateTimeOffset.MinValue;
        }

        public async Task<TunnelListResult> GetTunnelsAsync()
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new TunnelListResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                using var response = await _httpService.GetAsync(ProxyListApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[2/3] HTTP状态码: {(int)response.StatusCode} - {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = $"获取隧道列表失败: HTTP {response.StatusCode}\\n响应: {responseContent}";
                    Debug.WriteLine(error);
                    return new TunnelListResult
                    {
                        Success = false,
                        Message = $"获取隧道列表失败: HTTP {response.StatusCode}",
                        Error = responseContent
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var code = TryGetInt(root, "code");
                var message = TryGetString(root, "message") ?? string.Empty;

                if (code != 200)
                {
                    return new TunnelListResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "获取隧道列表失败" : message,
                        Error = responseContent
                    };
                }

                if (!root.TryGetProperty("data", out var dataElement))
                {
                    return new TunnelListResult
                    {
                        Success = false,
                        Message = "返回数据缺少 data 字段",
                        Error = responseContent
                    };
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var nodeNameMap = new Dictionary<int, string>();
                if (TryGetProperty(dataElement, "nodes", out var nodesElement) && nodesElement.ValueKind == JsonValueKind.Array)
                {
                    var nodes = DeserializeNodesSafe(nodesElement, options);
                    nodeNameMap = nodes
                        .GroupBy(n => n.nodeId)
                        .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.name ?? g.Key.ToString());
                }

                List<TunnelInfo> tunnels;
                if (TryGetProperty(dataElement, "proxies", out var proxiesElement) && proxiesElement.ValueKind == JsonValueKind.Array)
                {
                    tunnels = DeserializeTunnelsSafe(proxiesElement, options);
                }
                else if (dataElement.ValueKind == JsonValueKind.Array)
                {
                    tunnels = DeserializeTunnelsSafe(dataElement, options);
                }
                else
                {
                    tunnels = new List<TunnelInfo>();
                }

                foreach (var tunnel in tunnels)
                {
                    if (nodeNameMap.TryGetValue(tunnel.nodeId, out var nodeName) && !string.IsNullOrWhiteSpace(nodeName))
                    {
                        tunnel.NodeLabel = nodeName;
                    }
                }

                return new TunnelListResult
                {
                    Success = true,
                    Message = string.IsNullOrWhiteSpace(message) ? "获取隧道列表成功" : message,
                    Tunnels = tunnels
                };
            }
            catch (Exception ex)
            {
                return new TunnelListResult
                {
                    Success = false,
                    Message = "获取隧道列表异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<TunnelStartCommandResult> GetStartCommandAsync(int proxyId)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new TunnelStartCommandResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                using var response = await _httpService.GetAsync(UserFrpTokenApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new TunnelStartCommandResult
                    {
                        Success = false,
                        Message = $"获取启动参数失败: HTTP {(int)response.StatusCode}",
                        Error = responseContent
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var root = doc.RootElement;
                var code = TryGetInt(root, "code");
                var message = TryGetString(root, "message") ?? string.Empty;
                if (code != 200)
                {
                    return new TunnelStartCommandResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "获取启动参数失败" : message,
                        Error = responseContent
                    };
                }

                string command = string.Empty;
                if (TryGetProperty(root, "data", out var dataElement))
                {
                    // API returns token in data.token, then build start command locally.
                    if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        var frpToken = TryGetString(dataElement, "token")
                            ?? TryGetString(dataElement, "frpToken")
                            ?? TryGetString(dataElement, "accessKey")
                            ?? string.Empty;

                        if (!string.IsNullOrWhiteSpace(frpToken))
                        {
                            command = $"./mefrpc -n -t {frpToken.Trim()} -p {proxyId}";
                        }
                    }
                }

                return new TunnelStartCommandResult
                {
                    Success = !string.IsNullOrWhiteSpace(command),
                    Message = string.IsNullOrWhiteSpace(command)
                        ? (string.IsNullOrWhiteSpace(message) ? "获取启动参数失败" : message)
                        : (string.IsNullOrWhiteSpace(message) ? "获取启动参数成功" : message),
                    Command = command,
                    Error = string.IsNullOrWhiteSpace(command) ? responseContent : null
                };
            }
            catch (Exception ex)
            {
                return new TunnelStartCommandResult
                {
                    Success = false,
                    Message = "获取启动参数异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<TunnelLinkResult> GetTunnelLinkAsync(TunnelInfo tunnel)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new TunnelLinkResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                var protocol = (tunnel.proxyType ?? string.Empty).Trim().ToLowerInvariant();

                // HTTP/HTTPS 隧道优先使用隧道自带域名。
                if (protocol == "http" || protocol == "https")
                {
                    var domain = ResolvePrimaryDomain(tunnel.domain);
                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        return new TunnelLinkResult
                        {
                            Success = true,
                            Message = "获取链接成功",
                            Link = EnsureScheme(domain, protocol)
                        };
                    }
                }

                var nodeAddressResult = await GetNodeAddressAsync(tunnel.nodeId);
                if (!nodeAddressResult.Success || string.IsNullOrWhiteSpace(nodeAddressResult.Address))
                {
                    return new TunnelLinkResult
                    {
                        Success = false,
                        Message = nodeAddressResult.Message,
                        Error = nodeAddressResult.Error
                    };
                }

                var normalizedAddress = NormalizeAddress(nodeAddressResult.Address);
                string link;
                if (protocol == "http" || protocol == "https")
                {
                    link = EnsureScheme(normalizedAddress, protocol);
                }
                else
                {
                    link = ComposeHostAndPort(normalizedAddress, tunnel.remotePort);
                }

                return new TunnelLinkResult
                {
                    Success = !string.IsNullOrWhiteSpace(link),
                    Message = !string.IsNullOrWhiteSpace(link) ? "获取链接成功" : "链接地址为空",
                    Link = link
                };
            }
            catch (Exception ex)
            {
                return new TunnelLinkResult
                {
                    Success = false,
                    Message = "获取链接地址异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceActionResult> KickProxyAsync(int proxyId)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                var payload = JsonSerializer.Serialize(new { proxyId });
                // 某些入口可能被 WAF 拦截(HTML 403)，按顺序切换端点重试。
                string[] endpoints =
                {
                    ProxyKickApiUrl,                    // https://api.mefrp.com/api/auth/proxy/kick
                    ApiRootUrl + "/auth/proxy/kick"    // https://api.mefrp.com/auth/proxy/kick
                };

                ServiceActionResult? lastFailure = null;
                foreach (var endpoint in endpoints)
                {
                    ServiceActionResult result = await PostActionToEndpointAsync(endpoint, payload, "强制下线");
                    if (result.Success)
                    {
                        return result;
                    }

                    bool looksLikeWafHtml = !string.IsNullOrWhiteSpace(result.Error)
                        && result.Error.TrimStart().StartsWith("<", StringComparison.Ordinal);
                    bool is403 = result.Message.Contains("HTTP 403", StringComparison.OrdinalIgnoreCase);
                    bool is404 = result.Message.Contains("HTTP 404", StringComparison.OrdinalIgnoreCase);
                    lastFailure = result;

                    if (looksLikeWafHtml || is403 || is404)
                    {
                        continue;
                    }

                    return result;
                }

                return lastFailure ?? new ServiceActionResult
                {
                    Success = false,
                    Message = "强制下线失败"
                };
            }
            catch (Exception ex)
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "强制下线异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceActionResult> ToggleProxyDisabledAsync(int proxyId, bool isDisabled)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                var payload = JsonSerializer.Serialize(new { proxyId, isDisabled });
                var actionName = isDisabled ? "禁用隧道" : "启用隧道";
                return await PostActionWithFallbackAsync(
                    actionName,
                    payload,
                    ProxyToggleApiUrl,
                    ApiRootUrl + "/auth/proxy/toggle");
            }
            catch (Exception ex)
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = (isDisabled ? "禁用隧道" : "启用隧道") + "异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceActionResult> UpdateProxyAsync(TunnelUpdateRequest request)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                var payload = JsonSerializer.Serialize(request);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpService.PostAsync(ProxyUpdateApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return ParseActionResponse(response, responseContent, "更新隧道");
            }
            catch (Exception ex)
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "更新隧道异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<ServiceActionResult> DeleteProxyAsync(int proxyId)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "未登录或 Token 为空"
                };
            }

            try
            {
                var payload = JsonSerializer.Serialize(new { proxyId });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpService.PostAsync(ProxyDeleteApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                return ParseActionResponse(response, responseContent, "删除隧道");
            }
            catch (Exception ex)
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = "删除隧道异常",
                    Error = ex.Message
                };
            }
        }

        private async Task<NodeAddressResult> GetNodeAddressAsync(int nodeId)
        {
            if (_nodeAddressCache != null && DateTimeOffset.Now - _nodeAddressCacheTime < NodeAddressCacheDuration && _nodeAddressCache.TryGetValue(nodeId, out var cachedAddress) && !string.IsNullOrWhiteSpace(cachedAddress))
            {
                return new NodeAddressResult
                {
                    Success = true,
                    Message = "获取节点地址成功",
                    Address = cachedAddress
                };
            }

            using var response = await _httpService.GetAsync(NodeNameListApiUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new NodeAddressResult
                {
                    Success = false,
                    Message = $"获取链接地址失败: HTTP {(int)response.StatusCode}",
                    Error = responseContent
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            var code = TryGetInt(root, "code");
            var message = TryGetString(root, "message") ?? string.Empty;
            if (code != 200)
            {
                return new NodeAddressResult
                {
                    Success = false,
                    Message = string.IsNullOrWhiteSpace(message) ? "获取链接地址失败" : message,
                    Error = responseContent
                };
            }

            if (!TryGetProperty(root, "data", out var dataElement))
            {
                return new NodeAddressResult
                {
                    Success = false,
                    Message = "返回数据缺少 data 字段",
                    Error = responseContent
                };
            }

            var map = BuildNodeAddressMap(dataElement);
            if (map.Count == 0)
            {
                return new NodeAddressResult
                {
                    Success = false,
                    Message = "未找到可用节点地址",
                    Error = responseContent
                };
            }

            _nodeAddressCache = map;
            _nodeAddressCacheTime = DateTimeOffset.Now;

            if (!map.TryGetValue(nodeId, out var address) || string.IsNullOrWhiteSpace(address))
            {
                return new NodeAddressResult
                {
                    Success = false,
                    Message = "未找到该隧道节点的链接地址",
                    Error = responseContent
                };
            }

            return new NodeAddressResult
            {
                Success = true,
                Message = "获取节点地址成功",
                Address = address
            };
        }

        private async Task<ServiceActionResult> PostActionToEndpointAsync(string endpoint, string payload, string actionName)
        {
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _httpService.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseActionResponse(response, responseContent, actionName);
        }

        private async Task<ServiceActionResult> PostActionWithFallbackAsync(string actionName, string payload, params string[] endpoints)
        {
            HttpStatusCode? lastStatusCode = null;
            string? lastBody = null;

            foreach (var endpoint in endpoints.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpService.PostAsync(endpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return ParseActionResponse(response, responseContent, actionName);
                }

                lastStatusCode = response.StatusCode;
                lastBody = responseContent;
                if (response.StatusCode != HttpStatusCode.NotFound)
                {
                    return ParseActionResponse(response, responseContent, actionName);
                }
            }

            return new ServiceActionResult
            {
                Success = false,
                Message = lastStatusCode.HasValue
                    ? $"{actionName}失败: HTTP {(int)lastStatusCode.Value}"
                    : $"{actionName}失败",
                Error = lastBody
            };
        }

        private static ServiceActionResult ParseActionResponse(HttpResponseMessage response, string responseContent, string actionName)
        {
            if (!response.IsSuccessStatusCode)
            {
                return new ServiceActionResult
                {
                    Success = false,
                    Message = $"{actionName}失败: HTTP {(int)response.StatusCode}",
                    Error = responseContent
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;
            var code = TryGetInt(root, "code");
            var message = TryGetString(root, "message") ?? string.Empty;
            return new ServiceActionResult
            {
                Success = code == 200,
                Message = string.IsNullOrWhiteSpace(message) ? (code == 200 ? $"{actionName}成功" : $"{actionName}失败") : message,
                Error = code == 200 ? null : responseContent
            };
        }

        private static bool TryGetProperty(JsonElement root, string propertyName, out JsonElement property)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                property = default;
                return false;
            }

            if (root.TryGetProperty(propertyName, out property))
            {
                return true;
            }

            foreach (var item in root.EnumerateObject())
            {
                if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }
            }

            property = default;
            return false;
        }

        private static List<TunnelNodeInfo> DeserializeNodesSafe(JsonElement nodesElement, JsonSerializerOptions options)
        {
            try
            {
                return JsonSerializer.Deserialize<List<TunnelNodeInfo>>(nodesElement.GetRawText(), options) ?? new List<TunnelNodeInfo>();
            }
            catch
            {
                var result = new List<TunnelNodeInfo>();
                foreach (var item in nodesElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    result.Add(new TunnelNodeInfo
                    {
                        nodeId = TryGetInt(item, "nodeId"),
                        name = TryGetString(item, "name") ?? string.Empty,
                        hostname = TryGetString(item, "hostname") ?? string.Empty
                    });
                }

                return result;
            }
        }

        private static List<TunnelInfo> DeserializeTunnelsSafe(JsonElement tunnelsElement, JsonSerializerOptions options)
        {
            try
            {
                return JsonSerializer.Deserialize<List<TunnelInfo>>(tunnelsElement.GetRawText(), options) ?? new List<TunnelInfo>();
            }
            catch
            {
                var result = new List<TunnelInfo>();
                foreach (var item in tunnelsElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    result.Add(new TunnelInfo
                    {
                        proxyId = TryGetInt(item, "proxyId"),
                        proxyName = TryGetString(item, "proxyName") ?? string.Empty,
                        proxyType = TryGetString(item, "proxyType") ?? string.Empty,
                        localIp = TryGetString(item, "localIp") ?? string.Empty,
                        localPort = TryGetInt(item, "localPort"),
                        remotePort = TryGetInt(item, "remotePort"),
                        nodeId = TryGetInt(item, "nodeId"),
                        isOnline = TryGetBool(item, "isOnline"),
                        domain = TryGetString(item, "domain") ?? string.Empty,
                        lastStartTime = TryGetLong(item, "lastStartTime"),
                        lastCloseTime = TryGetLong(item, "lastCloseTime"),
                        clientVersion = TryGetString(item, "clientVersion") ?? string.Empty,
                        isBanned = TryGetBool(item, "isBanned"),
                        isDisabled = TryGetBool(item, "isDisabled")
                    });
                }

                return result;
            }
        }

        private static int TryGetInt(JsonElement root, string name)
        {
            if (!TryGetProperty(root, name, out var element))
            {
                return 0;
            }
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
            {
                return number;
            }
            if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out number))
            {
                return number;
            }
            return 0;
        }

        private static string? TryGetString(JsonElement root, string name)
        {
            if (!TryGetProperty(root, name, out var element))
            {
                return null;
            }
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }
            return element.ValueKind == JsonValueKind.Number ? element.GetRawText() : null;
        }

        private static long TryGetLong(JsonElement root, string name)
        {
            if (!TryGetProperty(root, name, out var element))
            {
                return 0;
            }
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out var number))
            {
                return number;
            }
            if (element.ValueKind == JsonValueKind.String && long.TryParse(element.GetString(), out number))
            {
                return number;
            }
            return 0;
        }

        private static bool TryGetBool(JsonElement root, string name)
        {
            if (!TryGetProperty(root, name, out var element))
            {
                return false;
            }
            if (element.ValueKind == JsonValueKind.True)
            {
                return true;
            }
            if (element.ValueKind == JsonValueKind.False)
            {
                return false;
            }
            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
            {
                return number == 1;
            }
            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString()?.Trim() ?? string.Empty;
                if (bool.TryParse(value, out var boolValue))
                {
                    return boolValue;
                }
                if (int.TryParse(value, out number))
                {
                    return number == 1;
                }
            }
            return false;
        }

        private static bool TryResolveNodeAddress(JsonElement dataElement, int nodeId, out string address)
        {
            address = string.Empty;

            if (dataElement.ValueKind == JsonValueKind.Object)
            {
                if (TryGetProperty(dataElement, nodeId.ToString(), out var byNodeIdElement))
                {
                    var mappedAddress = ExtractAddress(byNodeIdElement);
                    if (!string.IsNullOrWhiteSpace(mappedAddress))
                    {
                        address = mappedAddress!;
                        return true;
                    }
                }

                if (TryGetInt(dataElement, "nodeId") == nodeId)
                {
                    var objectAddress = ExtractAddress(dataElement);
                    if (!string.IsNullOrWhiteSpace(objectAddress))
                    {
                        address = objectAddress!;
                        return true;
                    }
                }
            }

            if (dataElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in dataElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (TryGetInt(item, "nodeId") != nodeId)
                {
                    continue;
                }

                var itemAddress = ExtractAddress(item);
                if (!string.IsNullOrWhiteSpace(itemAddress))
                {
                    address = itemAddress!;
                    return true;
                }
            }

            return false;
        }

        private static Dictionary<int, string> BuildNodeAddressMap(JsonElement dataElement)
        {
            var map = new Dictionary<int, string>();

            if (dataElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dataElement.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    var id = TryGetInt(item, "nodeId");
                    var address = ExtractAddress(item);
                    if (id > 0 && !string.IsNullOrWhiteSpace(address))
                    {
                        map[id] = address!;
                    }
                }
                return map;
            }

            if (dataElement.ValueKind != JsonValueKind.Object)
            {
                return map;
            }

            foreach (var property in dataElement.EnumerateObject())
            {
                if (!int.TryParse(property.Name, out var id))
                {
                    continue;
                }

                var address = ExtractAddress(property.Value);
                if (!string.IsNullOrWhiteSpace(address))
                {
                    map[id] = address!;
                }
            }

            return map;
        }

        private static string? ExtractAddress(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                return element.GetString();
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string[] keys =
            {
                "link",
                "url",
                "address",
                "connectAddress",
                "hostname",
                "host",
                "domain",
                "name"
            };

            foreach (var key in keys)
            {
                var value = TryGetString(element, key);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static string NormalizeAddress(string raw)
        {
            var value = raw.Trim();

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                return uri.Host;
            }

            var slashIndex = value.IndexOf('/');
            if (slashIndex > 0)
            {
                value = value.Substring(0, slashIndex);
            }

            return value;
        }

        private static string ComposeHostAndPort(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return string.Empty;
            }

            if (HasExplicitPort(host) || port <= 0)
            {
                return host;
            }

            return $"{host}:{port}";
        }

        private static bool HasExplicitPort(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            if (host.StartsWith("[", StringComparison.Ordinal) && host.Contains("]:", StringComparison.Ordinal))
            {
                return true;
            }

            var firstColon = host.IndexOf(':');
            if (firstColon < 0)
            {
                return false;
            }

            if (host.IndexOf(':', firstColon + 1) >= 0)
            {
                return false;
            }

            return int.TryParse(host[(firstColon + 1)..], out _);
        }

        private static string EnsureScheme(string address, string protocol)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return string.Empty;
            }

            if (address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                address.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return address;
            }

            return $"{protocol}://{address}";
        }

        private static string ResolvePrimaryDomain(string rawDomain)
        {
            var domains = ParseDomains(rawDomain);
            return domains.FirstOrDefault() ?? string.Empty;
        }

        private static string[] ParseDomains(string rawDomain)
        {
            var text = (rawDomain ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            if (!TryCollectDomainsFromJson(text, result, 0))
            {
                AddSplitDomains(text, result);
            }

            return result
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static bool TryCollectDomainsFromJson(string text, List<string> result, int depth)
        {
            if (depth > 2 || string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(text);
                CollectDomainsFromElement(doc.RootElement, result, depth);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CollectDomainsFromElement(JsonElement element, List<string> result, int depth)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                {
                    var text = element.GetString()?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        return;
                    }

                    if (LooksLikeJsonValue(text) && TryCollectDomainsFromJson(text, result, depth + 1))
                    {
                        return;
                    }

                    AddSplitDomains(text, result);
                    return;
                }
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        CollectDomainsFromElement(item, result, depth);
                    }
                    return;
                case JsonValueKind.Object:
                {
                    foreach (var key in new[] { "domain", "domains", "host", "hostname", "url", "value", "name" })
                    {
                        if (TryGetProperty(element, key, out var property))
                        {
                            CollectDomainsFromElement(property, result, depth);
                        }
                    }
                    return;
                }
                default:
                    return;
            }
        }

        private static bool LooksLikeJsonValue(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var first = text[0];
            return first == '[' || first == '{' || first == '"';
        }

        private static void AddSplitDomains(string text, List<string> result)
        {
            var values = text.Split(new[] { ',', ';', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var value in values)
            {
                var normalized = NormalizeDomainValue(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    result.Add(normalized);
                }
            }
        }

        private static string NormalizeDomainValue(string value)
        {
            var text = (value ?? string.Empty).Trim().Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(text, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host))
            {
                return uri.IsDefaultPort ? uri.Host : $"{uri.Host}:{uri.Port}";
            }

            var slashIndex = text.IndexOf('/');
            if (slashIndex >= 0)
            {
                text = text.Substring(0, slashIndex);
            }

            return text.Trim();
        }
    }

    public class TunnelInfo
    {
        public int proxyId { get; set; }
        public string proxyName { get; set; } = string.Empty;
        public string proxyType { get; set; } = string.Empty;
        public string localIp { get; set; } = string.Empty;
        public int localPort { get; set; }
        public int remotePort { get; set; }
        public int nodeId { get; set; }
        public bool isOnline { get; set; }
        public string domain { get; set; } = string.Empty;
        public long lastStartTime { get; set; }
        public long lastCloseTime { get; set; }
        public string clientVersion { get; set; } = string.Empty;
        public bool isBanned { get; set; }
        public bool isDisabled { get; set; }
        public bool IsLocalRunning { get; set; }
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraFields { get; set; }

        public int Id => proxyId;
        public string IdDisplayText => $"#{proxyId}";
        public string Name => proxyName;
        public string Type => proxyType;
        public string Protocol => proxyType;
        public string LocalAddr => $"{localIp}:{localPort}";
        public string RemoteAddr => domain;
        public string? NodeLabel { get; set; }
        public string NodeName => !string.IsNullOrWhiteSpace(NodeLabel) ? NodeLabel! : nodeId.ToString();
        public string RouteDisplayText => $"{LocalAddr} -> {(!string.IsNullOrWhiteSpace(RemoteAddr) ? RemoteAddr : remotePort.ToString())}";
        public string NodeDisplayText => $"#{nodeId} - {NodeName}";
        public bool IsDisabledResolved
        {
            get
            {
                if (isDisabled || isBanned)
                {
                    return true;
                }

                if (TryGetExtraBoolean("disabled", out var disabledValue) && disabledValue)
                {
                    return true;
                }

                if (TryGetExtraBoolean("isDisabled", out var isDisabledValue) && isDisabledValue)
                {
                    return true;
                }

                if (TryGetExtraBoolean("banned", out var bannedValue) && bannedValue)
                {
                    return true;
                }

                if (TryGetExtraBoolean("isBanned", out var isBannedValue) && isBannedValue)
                {
                    return true;
                }

                return false;
            }
        }
        public bool IsOnlineResolved
        {
            get
            {
                if (isOnline)
                {
                    return true;
                }

                if (TryGetExtraBoolean("online", out var onlineValue))
                {
                    return onlineValue;
                }

                if (TryGetExtraStatusOnline("status", out var statusOnline))
                {
                    return statusOnline;
                }

                if (TryGetExtraStatusOnline("proxyStatus", out var proxyStatusOnline))
                {
                    return proxyStatusOnline;
                }

                if (TryGetExtraStatusOnline("runStatus", out var runStatusOnline))
                {
                    return runStatusOnline;
                }

                return false;
            }
        }
        public int Status => IsOnlineResolved ? 1 : 2;
        public string OnlineStatusText => IsOnlineResolved ? "在线" : "离线";

        private bool TryGetExtraBoolean(string key, out bool value)
        {
            value = false;
            if (ExtraFields == null || !TryGetExtraField(key, out var element))
            {
                return false;
            }

            if (element.ValueKind == JsonValueKind.True)
            {
                value = true;
                return true;
            }
            if (element.ValueKind == JsonValueKind.False)
            {
                value = false;
                return true;
            }
            if (element.ValueKind == JsonValueKind.Number)
            {
                if (element.TryGetInt32(out var numericValue))
                {
                    value = numericValue == 1;
                    return true;
                }
                return false;
            }
            if (element.ValueKind == JsonValueKind.String)
            {
                var raw = element.GetString()?.Trim() ?? string.Empty;
                if (bool.TryParse(raw, out var boolValue))
                {
                    value = boolValue;
                    return true;
                }
                if (int.TryParse(raw, out var parsedValue))
                {
                    value = parsedValue == 1;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetExtraStatusOnline(string key, out bool value)
        {
            value = false;
            if (ExtraFields == null || !TryGetExtraField(key, out var element))
            {
                return false;
            }

            if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
            {
                value = numericValue == 1;
                return true;
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var raw = element.GetString()?.Trim() ?? string.Empty;
                if (int.TryParse(raw, out var parsedValue))
                {
                    value = parsedValue == 1;
                    return true;
                }
                if (string.Equals(raw, "online", StringComparison.OrdinalIgnoreCase))
                {
                    value = true;
                    return true;
                }
                if (string.Equals(raw, "offline", StringComparison.OrdinalIgnoreCase))
                {
                    value = false;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetExtraField(string key, out JsonElement value)
        {
            if (ExtraFields != null)
            {
                foreach (var kv in ExtraFields)
                {
                    if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                    {
                        value = kv.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }
    }

    public class TunnelNodeInfo
    {
        public int nodeId { get; set; }
        public string name { get; set; } = string.Empty;
        public string hostname { get; set; } = string.Empty;
    }

    public class TunnelListResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<TunnelInfo>? Tunnels { get; set; }
        public string? Error { get; set; }
    }

    public class TunnelStartCommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Command { get; set; }
        public string? Error { get; set; }
    }

    public class TunnelLinkResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public string? Error { get; set; }
    }

    public class ServiceActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class TunnelUpdateRequest
    {
        public int proxyId { get; set; }
        public int nodeId { get; set; }
        public string proxyName { get; set; } = string.Empty;
        public string localIp { get; set; } = string.Empty;
        public int localPort { get; set; }
        public int remotePort { get; set; }
        public string domain { get; set; } = string.Empty;
        public string location { get; set; } = string.Empty;
        public string accessKey { get; set; } = string.Empty;
        public string httpPlugin { get; set; } = string.Empty;
        public string hostHeaderRewrite { get; set; } = string.Empty;
        public Dictionary<string, string> requestHeaders { get; set; } = new();
        public Dictionary<string, string> responseHeaders { get; set; } = new();
        public string httpUser { get; set; } = string.Empty;
        public string httpPassword { get; set; } = string.Empty;
        public string crtPath { get; set; } = string.Empty;
        public string keyPath { get; set; } = string.Empty;
        public bool useEncryption { get; set; }
        public bool useCompression { get; set; }
        public string proxyProtocolVersion { get; set; } = string.Empty;
        public string proxyType { get; set; } = string.Empty;
        public string transportProtocol { get; set; } = "tcp";
        public string locations { get; set; } = string.Empty;
    }

    internal class NodeAddressResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Error { get; set; }
    }
}
