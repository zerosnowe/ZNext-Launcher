using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ZNext.Services
{
    public interface IHttpService
    {
        Task<HttpResponseMessage> GetAsync(string url);
        Task<HttpResponseMessage> PostAsync(string url, HttpContent? content = null);
        void SetAuthToken(string? token);
        void ClearAuthToken();
    }

    public class HttpService : IHttpService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private string? _authToken;
        private bool _disposed;

        public HttpService()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ZNext-WinUI3-App/1.0");
            _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://www.mefrp.com");
            _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://www.mefrp.com/");
        }

        public void SetAuthToken(string? token)
        {
            _authToken = token;
            UpdateAuthHeader();
        }

        public void ClearAuthToken()
        {
            _authToken = null;
            UpdateAuthHeader();
        }

        private void UpdateAuthHeader()
        {
            if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Authorization");
            }

            if (!string.IsNullOrEmpty(_authToken))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_authToken}");
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content = null)
        {
            return await _httpClient.PostAsync(url, content);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient.Dispose();
                _disposed = true;
            }
        }
    }
}
