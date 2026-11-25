using StaffSync.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaffSync.Services
{
    public class ApiException : Exception
    {
        public System.Net.HttpStatusCode StatusCode { get; }

        public ApiException(System.Net.HttpStatusCode statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    public class ApiOfflineException : Exception
    {
        public ApiOfflineException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

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

        public ApiClient(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
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
            HttpResponseMessage response;
            string? tokenUsed = null;
            try
            {
                var request = await requestFactory();
                tokenUsed = request.Headers.Authorization?.Parameter;
                response = await _httpClient.SendAsync(request);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException)
            {
                throw new ApiOfflineException("Ağ bağlantısı sağlanamadı.", ex);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                bool refreshed = false;
                try
                {
                    refreshed = await _authService.RefreshTokenAsync(tokenUsed);
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException || ex is ApiOfflineException)
                {
                    throw new ApiOfflineException("Ağ bağlantısı sağlanamadığı için oturum yenilenemedi.", ex);
                }

                if (refreshed)
                {
                    try
                    {
                        var retryRequest = await requestFactory();
                        response = await _httpClient.SendAsync(retryRequest);
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException)
                    {
                        throw new ApiOfflineException("Ağ bağlantısı sağlanamadı.", ex);
                    }
                }
                else
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Shell.Current.GoToAsync("//login");
                    });
                }
            }

            return response;
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            string errorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            try
            {
                var content = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("errors", out var errorsProp) && errorsProp.ValueKind == JsonValueKind.Array)
                    {
                        var errors = errorsProp.EnumerateArray()
                            .Select(e => e.GetString())
                            .Where(e => !string.IsNullOrEmpty(e))
                            .ToList();
                        if (errors.Count > 0)
                            errorMessage = string.Join("\n", errors);
                    }
                    else if (doc.RootElement.TryGetProperty("message", out var msgProp))
                    {
                        errorMessage = msgProp.GetString() ?? errorMessage;
                    }
                    else
                    {
                        errorMessage = content;
                    }
                }
            }
            catch
            {
                // JSON parse edilemezse varsayılan HTTP durum açıklaması kalır
            }

            throw new ApiException(response.StatusCode, errorMessage);
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await SendWithRetryAsync(() => CreateRequestAsync(HttpMethod.Get, endpoint));
            await EnsureSuccessAsync(response);

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions);
        }

        public async Task<TResult?> PostAsync<TRequest, TResult>(string endpoint, TRequest data)
        {
            var response = await SendWithRetryAsync(() =>
            {
                var content = JsonContent.Create(data);
                return CreateRequestAsync(HttpMethod.Post, endpoint, content);
            });

            await EnsureSuccessAsync(response);

            var stream = await response.Content.ReadAsStreamAsync();
            return await JsonSerializer.DeserializeAsync<TResult>(stream, _jsonOptions);
        }

        public async Task<bool> PostAsync<TRequest>(string endpoint, TRequest data)
        {
            var response = await SendWithRetryAsync(() =>
            {
                var content = JsonContent.Create(data);
                return CreateRequestAsync(HttpMethod.Post, endpoint, content);
            });

            await EnsureSuccessAsync(response);
            return true;
        }
    }
}
