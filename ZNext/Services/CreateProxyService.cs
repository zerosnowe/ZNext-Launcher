using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZNext.Services
{
    public class CreateProxyService
    {
        private const string BaseApiUrl = "https://api.mefrp.com/api";
        private readonly IHttpService _httpService;
        private string? _token;

        public CreateProxyService() : this(new HttpService())
        {
        }

        public CreateProxyService(IHttpService httpService)
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
        }

        public async Task<CreateProxyDataResult> GetCreateProxyDataAsync()
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new CreateProxyDataResult { Success = false, Message = "未登录或 Token 为空" };
            }

            try
            {
                using var response = await _httpService.GetAsync($"{BaseApiUrl}/auth/createProxyData");
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return new CreateProxyDataResult
                    {
                        Success = false,
                        Message = $"获取创建隧道数据失败: HTTP {(int)response.StatusCode}",
                        Error = body
                    };
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var code = TryGetInt(root, "code");
                var message = TryGetString(root, "message") ?? string.Empty;
                if (code != 200)
                {
                    return new CreateProxyDataResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "获取创建隧道数据失败" : message,
                        Error = body
                    };
                }

                if (!root.TryGetProperty("data", out var dataElement))
                {
                    return new CreateProxyDataResult
                    {
                        Success = false,
                        Message = "返回数据缺少 data 字段",
                        Error = body
                    };
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = JsonSerializer.Deserialize<CreateProxyDataDto>(dataElement.GetRawText(), options) ?? new CreateProxyDataDto();
                return new CreateProxyDataResult
                {
                    Success = true,
                    Message = string.IsNullOrWhiteSpace(message) ? "获取创建隧道数据成功" : message,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                return new CreateProxyDataResult
                {
                    Success = false,
                    Message = "获取创建隧道数据异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<FreeNodePortResult> GetFreeNodePortAsync(int nodeId, string protocol)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new FreeNodePortResult { Success = false, Message = "未登录或 Token 为空" };
            }

            try
            {
                var payload = JsonSerializer.Serialize(new { nodeId, protocol });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpService.PostAsync($"{BaseApiUrl}/auth/node/freePort", content);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return new FreeNodePortResult
                    {
                        Success = false,
                        Message = $"获取空闲端口失败: HTTP {(int)response.StatusCode}",
                        Error = body
                    };
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var code = TryGetInt(root, "code");
                var message = TryGetString(root, "message") ?? string.Empty;
                if (code != 200)
                {
                    return new FreeNodePortResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(message) ? "获取空闲端口失败" : message,
                        Error = body
                    };
                }

                var data = TryGetInt(root, "data");
                return new FreeNodePortResult
                {
                    Success = true,
                    Message = string.IsNullOrWhiteSpace(message) ? "获取空闲端口成功" : message,
                    Port = data
                };
            }
            catch (Exception ex)
            {
                return new FreeNodePortResult
                {
                    Success = false,
                    Message = "获取空闲端口异常",
                    Error = ex.Message
                };
            }
        }

        public async Task<CreateProxyResult> CreateProxyAsync(CreateProxyRequest request)
        {
            if (string.IsNullOrWhiteSpace(_token))
            {
                return new CreateProxyResult { Success = false, Message = "未登录或 Token 为空" };
            }

            try
            {
                var payload = JsonSerializer.Serialize(request);
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpService.PostAsync($"{BaseApiUrl}/auth/proxy/create", content);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    return new CreateProxyResult
                    {
                        Success = false,
                        Message = $"创建隧道失败: HTTP {(int)response.StatusCode}",
                        Error = body
                    };
                }

                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                var code = TryGetInt(root, "code");
                var message = TryGetString(root, "message") ?? string.Empty;
                return new CreateProxyResult
                {
                    Success = code == 200,
                    Message = string.IsNullOrWhiteSpace(message) ? (code == 200 ? "创建隧道成功" : "创建隧道失败") : message,
                    Error = body
                };
            }
            catch (Exception ex)
            {
                return new CreateProxyResult
                {
                    Success = false,
                    Message = "创建隧道异常",
                    Error = ex.Message
                };
            }
        }

        private static int TryGetInt(JsonElement root, string name)
        {
            if (!root.TryGetProperty(name, out var element))
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
            return root.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
                ? element.GetString()
                : null;
        }
    }

    public class CreateProxyDataResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public CreateProxyDataDto? Data { get; set; }
        public string? Error { get; set; }
    }

    public class FreeNodePortResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Port { get; set; }
        public string? Error { get; set; }
    }

    public class CreateProxyResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    public class CreateProxyDataDto
    {
        public List<CreateProxyNodeDto> nodes { get; set; } = new();
        public List<CreateProxyGroupDto> groups { get; set; } = new();
        public string currentGroup { get; set; } = string.Empty;
    }

    public class CreateProxyNodeDto
    {
        public int nodeId { get; set; }
        public string name { get; set; } = string.Empty;
        public string hostname { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public bool isOnline { get; set; }
        public string allowPort { get; set; } = "1-65535";
        public string allowType { get; set; } = "tcp;udp";
        public string allowGroup { get; set; } = string.Empty;
        public string region { get; set; } = string.Empty;
        public string bandwidth { get; set; } = string.Empty;
        public int loadPercent { get; set; }
    }

    public class CreateProxyGroupDto
    {
        public string name { get; set; } = string.Empty;
        public string friendlyName { get; set; } = string.Empty;
    }

    public class CreateProxyRequest
    {
        public int nodeId { get; set; }
        public string proxyName { get; set; } = string.Empty;
        public string localIp { get; set; } = string.Empty;
        public int localPort { get; set; }
        public int remotePort { get; set; }
        public string domain { get; set; } = string.Empty;
        public string proxyType { get; set; } = string.Empty;
        public string accessKey { get; set; } = string.Empty;
        public string httpPlugin { get; set; } = string.Empty;
        public string httpUser { get; set; } = string.Empty;
        public string httpPassword { get; set; } = string.Empty;
        public string crtPath { get; set; } = string.Empty;
        public string keyPath { get; set; } = string.Empty;
        public string proxyProtocolVersion { get; set; } = string.Empty;
        public bool useEncryption { get; set; }
        public bool useCompression { get; set; }
        public string transportProtocol { get; set; } = "tcp";
        public string locations { get; set; } = string.Empty;
        public string hostHeaderRewrite { get; set; } = string.Empty;
        public Dictionary<string, string> requestHeaders { get; set; } = new();
        public Dictionary<string, string> responseHeaders { get; set; } = new();
    }
}
