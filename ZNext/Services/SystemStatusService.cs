using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZNext.Services
{
    public class SystemStatusService
    {
        private const string ApiUrl = "https://api.mefrp.com/api/auth/system/status";
        private readonly IHttpService _httpService;

        public SystemStatusService() : this(new HttpService())
        {
        }

        public SystemStatusService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public void SetToken(string token)
        {
            _httpService.SetAuthToken(token);
        }

        public void ClearToken()
        {
            _httpService.ClearAuthToken();
        }

        public async Task<SystemStatusResult> GetSystemStatusAsync()
        {
            try
            {
                using var response = await _httpService.GetAsync(ApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new SystemStatusResult
                    {
                        Success = false,
                        Message = $"获取系统状态失败: HTTP {(int)response.StatusCode}",
                        Error = responseContent
                    };
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var apiResponse = JsonSerializer.Deserialize<SystemStatusApiResponse>(responseContent, options);

                if (apiResponse == null || apiResponse.Data == null)
                {
                    return new SystemStatusResult
                    {
                        Success = false,
                        Message = "系统状态解析失败",
                        Error = responseContent
                    };
                }

                return new SystemStatusResult
                {
                    Success = true,
                    Message = apiResponse.Message,
                    Status = apiResponse.Data
                };
            }
            catch (Exception ex)
            {
                return new SystemStatusResult
                {
                    Success = false,
                    Message = "获取系统状态异常",
                    Error = ex.Message
                };
            }
        }
    }

    public class SystemStatusApiResponse
    {
        public int Code { get; set; }
        public SystemStatusData? Data { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SystemStatusData
    {
        public int Status { get; set; }
        public string Remark { get; set; } = string.Empty;
    }

    public class SystemStatusResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public SystemStatusData? Status { get; set; }
    }
}
