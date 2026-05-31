using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZNext.Infrastructure.Settings;

namespace ZNext.Services
{
    public class AuthService
    {
        private const string LoginApiUrl = "https://api.mefrp.com/api/public/login";
        private static readonly string[] TokenFieldNames =
        [
            "token",
            "accessToken",
            "access_token",
            "authToken",
            "auth_token",
            "bearerToken",
            "bearer_token",
            "jwt"
        ];

        private readonly IAppSettingsStore _settingsStore;
        private readonly AuthCredentialStore _credentialStore;
        private string? _token;

        public string? Token
        {
            get => _token;
            private set => _token = value;
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(_token);

        public AuthService()
            : this(new AppSettingsStore())
        {
        }

        internal AuthService(IAppSettingsStore settingsStore)
        {
            _settingsStore = settingsStore;
            _credentialStore = new AuthCredentialStore(_settingsStore);
            LoadTokenFromSettings();
        }

        private void LoadTokenFromSettings()
        {
            try
            {
                _token = _credentialStore.LoadToken();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Load token failed: {ex.Message}");
            }
        }

        private void SaveTokenToSettings(bool persist)
        {
            try
            {
                if (!string.IsNullOrEmpty(_token) && persist)
                {
                    _credentialStore.SaveToken(_token);
                }
                else
                {
                    _credentialStore.ClearToken();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save token failed: {ex.Message}");
            }
        }

        public void SetToken(string token, bool persist = true)
        {
            _token = token;
            SaveTokenToSettings(persist);
        }

        public void ClearToken()
        {
            _token = null;
            SaveTokenToSettings(persist: true);
        }

        public bool LoadRememberLoginPreference()
        {
            return _credentialStore.LoadRememberLogin();
        }

        public string? LoadRememberedUsername()
        {
            return _credentialStore.LoadRememberedUsername();
        }

        public void SaveLoginPreferences(string? username, bool rememberLogin)
        {
            _credentialStore.SaveLoginPreferences(username, rememberLogin);
        }

        public async Task<LoginResult> LoginAsync(string username, string password, string captchaToken, bool rememberLogin = true)
        {
            try
            {
                using var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ZNext-WinUI3-App/1.0");
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                var normalizedCaptchaToken = NormalizeCaptchaToken(captchaToken);
                var payload = new
                {
                    username,
                    password,
                    captchaToken = normalizedCaptchaToken
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(LoginApiUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = $"登录失败: HTTP {(int)response.StatusCode}",
                        Error = responseContent
                    };
                }

                using var json = JsonDocument.Parse(responseContent);
                var root = json.RootElement;
                var code = TryGetInt(root, "code");
                var message = TryGetString(root, "message") ?? "登录失败";

                if (code.HasValue && code.Value != 200)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = message,
                        Error = responseContent
                    };
                }

                var token = ExtractToken(root) ?? ExtractTokenFromHeaders(response);
                if (string.IsNullOrWhiteSpace(token))
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "登录成功但未获取到 Token",
                        Error = responseContent
                    };
                }

                if (!string.IsNullOrWhiteSpace(token))
                {
                    SetToken(token, rememberLogin);
                }

                return new LoginResult
                {
                    Success = true,
                    Message = message,
                    Token = token,
                    Error = responseContent
                };
            }
            catch (HttpRequestException ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "网络请求失败",
                    Error = ex.Message
                };
            }
            catch (TaskCanceledException ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "登录超时",
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "登录异常",
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
                // Input is not Base64, keep original token.
            }

            var parts = token.Split(new[] { "||" }, StringSplitOptions.None);
            return parts.Length > 0 ? parts[0].Trim() : token;
        }

        private static int? TryGetInt(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
            {
                return number;
            }

            return null;
        }

        private static string? TryGetString(JsonElement obj, string name)
        {
            return obj.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }

        private static string? ExtractToken(JsonElement root)
        {
            var topLevelToken = TryTokenFields(root);
            if (!string.IsNullOrWhiteSpace(topLevelToken))
            {
                return topLevelToken;
            }

            if (!root.TryGetProperty("data", out var data))
            {
                return null;
            }

            if (data.ValueKind == JsonValueKind.String)
            {
                var token = data.GetString();
                return string.IsNullOrWhiteSpace(token) ? null : token;
            }

            if (data.ValueKind == JsonValueKind.Object)
            {
                string? dataToken = TryTokenFields(data);
                if (!string.IsNullOrWhiteSpace(dataToken))
                {
                    return dataToken;
                }
            }

            return TryTokenFieldsRecursive(root);
        }

        private static string? TryTokenFields(JsonElement obj)
        {
            foreach (string fieldName in TokenFieldNames)
            {
                string? token = TryGetString(obj, fieldName);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token;
                }
            }

            return null;
        }

        private static string? TryTokenFieldsRecursive(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        if (IsTokenFieldName(property.Name)
                            && property.Value.ValueKind == JsonValueKind.String)
                        {
                            string? token = property.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(token))
                            {
                                return token;
                            }
                        }

                        string? nestedToken = TryTokenFieldsRecursive(property.Value);
                        if (!string.IsNullOrWhiteSpace(nestedToken))
                        {
                            return nestedToken;
                        }
                    }

                    break;

                case JsonValueKind.Array:
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        string? nestedToken = TryTokenFieldsRecursive(item);
                        if (!string.IsNullOrWhiteSpace(nestedToken))
                        {
                            return nestedToken;
                        }
                    }

                    break;
            }

            return null;
        }

        private static bool IsTokenFieldName(string name)
        {
            return TokenFieldNames.Any(fieldName => string.Equals(fieldName, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ExtractTokenFromHeaders(HttpResponseMessage response)
        {
            if (response.Headers.TryGetValues("Authorization", out var authValues))
            {
                var authHeader = authValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader))
                {
                    const string bearerPrefix = "Bearer ";
                    if (authHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader[bearerPrefix.Length..].Trim();
                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            return token;
                        }
                    }
                }
            }

            if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
            {
                foreach (var cookie in cookieValues)
                {
                    var segments = cookie.Split(';', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var segment in segments)
                    {
                        var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);
                        if (parts.Length == 2 &&
                            (parts[0].Equals("token", StringComparison.OrdinalIgnoreCase) ||
                             parts[0].Equals("access_token", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!string.IsNullOrWhiteSpace(parts[1]))
                            {
                                return parts[1];
                            }
                        }
                    }
                }
            }

            return null;
        }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public string? Error { get; set; }
    }
}
