using StaffSync.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaffSync.Services
{
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<TResult?> PostAsync<TRequest, TResult>(string endpoint, TRequest data);
        Task<bool> PostAsync<TRequest>(string endpoint, TRequest data);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient httpClient, IAuthService _authService)
        {
            _httpClient = httpClient;
            this._authService = _authService;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string endpoint, HttpContent? content = null)
        {
            var baseUrl = ApiConfig.BaseUrl.TrimEnd('/');
            var url = endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase) 
                ? endpoint 
                : $"{baseUrl}/api/{endpoint.TrimStart('/')}";

            var request = new HttpRequestMessage(method, url);

            var token = await _authService.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            if (content != null)
                request.Content = content;

            return request;
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpRequestMessage>> requestFactory)
        {
            var request = await requestFactory();
            var response = await _httpClient.SendAsync(request);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token yenilemeyi dene
                var refreshed = await _authService.RefreshTokenAsync();
                if (refreshed)
                {
                    // Yeni token ile bir kez daha dene
                    var retryRequest = await requestFactory();
                    response = await _httpClient.SendAsync(retryRequest);
                }
                else
                {
                    // Oturum açma ekranına yönlendir
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.GoToAsync("//login");
                    });
                }
            }

            return response;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await SendWithRetryAsync(() => CreateRequestAsync(HttpMethod.Get, endpoint));
                if (!response.IsSuccessStatusCode)
                    return default;

                var stream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        public async Task<TResult?> PostAsync<TRequest, TResult>(string endpoint, TRequest data)
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var content = JsonContent.Create(data);
                    return CreateRequestAsync(HttpMethod.Post, endpoint, content);
                });

                if (!response.IsSuccessStatusCode)
                    return default;

                var stream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<TResult>(stream, _jsonOptions);
            }
            catch
            {
                return default;
            }
        }

        public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
        {
            try
            {
                var response = await SendWithRetryAsync(() =>
                {
                    var content = JsonContent.Create(data);
                    return CreateRequestAsync(HttpMethod.Post, endpoint, content);
                });

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
