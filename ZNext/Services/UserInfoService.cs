using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;

namespace ZNext.Services
{
    public class UserInfoService
    {
        private const string UserInfoApiUrl = "https://api.mefrp.com/api/auth/user/info";
        private const string SignApiUrl = "https://api.mefrp.com/api/auth/user/sign";
        private const string UserInfoCacheFileName = "user_info_cache.json";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpService _httpService;
        private string? _token;

        public UserInfoService() : this(new HttpService())
        {
        }

        public UserInfoService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpService.SetAuthToken(token);
            Debug.WriteLine("UserInfoService token set");
        }

        public void ClearToken()
        {
            _token = null;
            _httpService.ClearAuthToken();
            Debug.WriteLine("UserInfoService token cleared");
        }

        public async Task<UserInfoData?> GetCachedUserInfoAsync()
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var item = await folder.TryGetItemAsync(UserInfoCacheFileName);
                if (item is not StorageFile file)
                {
                    return null;
                }

                var json = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }

                var cache = JsonSerializer.Deserialize<UserInfoCacheData>(json, JsonOptions);
                return cache?.UserInfo;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Read user info cache failed: {ex.Message}");
                return null;
            }
        }

        public async Task SaveCachedUserInfoAsync(UserInfoData userInfo)
        {
            try
            {
                var cache = new UserInfoCacheData
                {
                    CachedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    UserInfo = userInfo
                };

                var json = JsonSerializer.Serialize(cache);
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                    UserInfoCacheFileName,
                    CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save user info cache failed: {ex.Message}");
            }
        }

        public async Task ClearCachedUserInfoAsync()
        {
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var item = await folder.TryGetItemAsync(UserInfoCacheFileName);
                if (item is StorageFile file)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Clear user info cache failed: {ex.Message}");
            }
        }

        public async Task<UserInfoResult> GetUserInfoAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_token))
                {
                    return new UserInfoResult
                    {
                        Success = false,
                        Message = "未登录或 Token 为空"
                    };
                }

                using var response = await _httpService.GetAsync(UserInfoApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new UserInfoResult
                    {
                        Success = false,
                        Message = $"获取用户信息失败: HTTP {response.StatusCode}",
                        Error = responseContent
                    };
                }

                var apiResponse = JsonSerializer.Deserialize<UserInfoApiResponse>(responseContent, JsonOptions);
                if (apiResponse == null)
                {
                    return new UserInfoResult
                    {
                        Success = false,
                        Message = "无法解析 API 响应",
                        Error = responseContent
                    };
                }

                if (apiResponse.Code != 200)
                {
                    return new UserInfoResult
                    {
                        Success = false,
                        Message = apiResponse.Message,
                        Error = responseContent
                    };
                }

                if (apiResponse.Data == null)
                {
                    return new UserInfoResult
                    {
                        Success = false,
                        Message = "API 返回数据为空"
                    };
                }

                await SaveCachedUserInfoAsync(apiResponse.Data);

                return new UserInfoResult
                {
                    Success = true,
                    Message = apiResponse.Message,
                    UserInfo = apiResponse.Data
                };
            }
            catch (HttpRequestException ex)
            {
                return new UserInfoResult
                {
                    Success = false,
                    Message = "网络连接失败，请检查网络设置",
                    Error = ex.Message
                };
            }
            catch (TaskCanceledException ex)
            {
                return new UserInfoResult
                {
                    Success = false,
                    Message = "请求超时，请稍后重试",
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new UserInfoResult
                {
                    Success = false,
                    Message = "获取用户信息过程中发生错误",
                    Error = ex.Message
                };
            }
        }

        public async Task<SignResult> SignWithCaptchaAsync(string captchaToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_token))
                {
                    return new SignResult
                    {
                        Success = false,
                        Message = "未登录或 Token 为空"
                    };
                }

                var normalizedToken = NormalizeCaptchaToken(captchaToken);
                if (string.IsNullOrWhiteSpace(normalizedToken))
                {
                    return new SignResult
                    {
                        Success = false,
                        Message = "验证码无效"
                    };
                }

                var payload = JsonSerializer.Serialize(new { captchaToken = normalizedToken });
                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await _httpService.PostAsync(SignApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new SignResult
                    {
                        Success = false,
                        Message = $"签到失败: HTTP {(int)response.StatusCode}",
                        Error = responseContent
                    };
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiResponse = JsonSerializer.Deserialize<CommonApiResponse>(responseContent, options);
                if (apiResponse == null)
                {
                    return new SignResult
                    {
                        Success = false,
                        Message = "签到响应解析失败",
                        Error = responseContent
                    };
                }

                return new SignResult
                {
                    Success = apiResponse.Code == 200,
                    Message = string.IsNullOrWhiteSpace(apiResponse.Message)
                        ? (apiResponse.Code == 200 ? "签到成功" : "签到失败")
                        : apiResponse.Message,
                    Error = responseContent
                };
            }
            catch (Exception ex)
            {
                return new SignResult
                {
                    Success = false,
                    Message = "签到请求异常",
                    Error = ex.Message
                };
            }
        }

        private static string NormalizeCaptchaToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return string.Empty;
            }

            var token = rawToken.Trim();
            try
            {
                var bytes = Convert.FromBase64String(token);
                var decoded = Encoding.UTF8.GetString(bytes).Trim();
                if (!string.IsNullOrWhiteSpace(decoded))
                {
                    token = decoded;
                }
            }
            catch
            {
                // keep raw token if it's not base64
            }

            var parts = token.Split(new[] { "||" }, StringSplitOptions.None);
            return parts.Length > 0 ? parts[0].Trim() : token;
        }

    }

    public class UserInfoCacheData
    {
        public long CachedAt { get; set; }
        public UserInfoData? UserInfo { get; set; }
    }

    public class UserInfoApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserInfoData? Data { get; set; }
    }

    public class UserInfoData
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FriendlyGroup { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public bool IsRealname { get; set; }
        public int Status { get; set; }
        public long RegTime { get; set; }
        public long Traffic { get; set; }
        public JsonElement InBound { get; set; }
        public JsonElement OutBound { get; set; }
        public int MaxProxies { get; set; }
        public int UsedProxies { get; set; }
        public bool TodaySigned { get; set; }
    }

    public class UserInfoResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserInfoData? UserInfo { get; set; }
        public string? Error { get; set; }
    }

    public class CommonApiResponse
    {
        public int Code { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class SignResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

}
