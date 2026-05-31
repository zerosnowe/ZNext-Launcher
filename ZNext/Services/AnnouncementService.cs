using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ZNext.Services
{
    public class AnnouncementService
    {
        private const string ApiUrl = "https://api.mefrp.com/api/auth/notice";
        private const string PopupApiUrl = "https://api.mefrp.com/api/auth/popupNotice";
        private string? _bearerToken;

        public void SetToken(string token)
        {
            _bearerToken = token;
        }

        public void ClearToken()
        {
            _bearerToken = null;
        }

        public async Task<string> GetAnnouncementAsync()
        {
            if (string.IsNullOrWhiteSpace(_bearerToken))
            {
                return "NO_ANNOUNCEMENT";
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                };

                using var httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_bearerToken}");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ZNext-WinUI3-App/1.0");
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                var response = await httpClient.GetAsync(ApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Announcement HTTP failed: {(int)response.StatusCode} {response.StatusCode}");
                    Debug.WriteLine(responseContent);
                    return $"ERROR: HTTP {(int)response.StatusCode}";
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, options);
                if (result == null)
                {
                    return "ERROR: invalid response";
                }

                if (result.Code != 200)
                {
                    return $"ERROR: {result.Message}";
                }

                var content = result.Data;
                if (string.IsNullOrWhiteSpace(content))
                {
                    return "NO_ANNOUNCEMENT";
                }

                return content;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Announcement fetch exception: {ex.Message}");
                return "ERROR: network exception";
            }
        }

        public async Task<string> GetPopupAnnouncementAsync()
        {
            if (string.IsNullOrWhiteSpace(_bearerToken))
            {
                return "NO_ANNOUNCEMENT";
            }

            try
            {
                using var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxAutomaticRedirections = 10
                };

                using var httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_bearerToken}");
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ZNext-WinUI3-App/1.0");
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                var response = await httpClient.GetAsync(PopupApiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"Popup announcement HTTP failed: {(int)response.StatusCode} {response.StatusCode}");
                    Debug.WriteLine(responseContent);
                    return $"ERROR: HTTP {(int)response.StatusCode}";
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<ApiResponse>(responseContent, options);
                if (result == null)
                {
                    return "ERROR: invalid response";
                }

                if (result.Code != 200)
                {
                    return $"ERROR: {result.Message}";
                }

                if (string.IsNullOrWhiteSpace(result.Data))
                {
                    return "NO_ANNOUNCEMENT";
                }

                return result.Data;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Popup announcement fetch exception: {ex.Message}");
                return "ERROR: network exception";
            }
        }
    }

    public class ApiResponse
    {
        public int Code { get; set; }
        public string Data { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
