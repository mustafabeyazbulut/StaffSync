using StaffSync.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace StaffSync.Services
{
    public interface IAuthService
    {
        Task<bool> IsAuthenticatedAsync();
        Task<string?> GetAccessTokenAsync();
        Task<string?> GetRefreshTokenAsync();
        Task<bool> LoginAsync(string email, string password);
        Task<bool> RefreshTokenAsync(string? currentAccessToken = null);
        Task LogoutAsync();
        Task<UserInfo?> GetCurrentUserAsync();
    }

    public class AuthService : IAuthService
    {
        private const string AccessTokenKey = "StaffSync_AccessToken";
        private const string RefreshTokenKey = "StaffSync_RefreshToken";
        private const string ExpirationKey = "StaffSync_TokenExpiration";

        private readonly HttpClient _httpClient;
        private readonly SemaphoreSlim _refreshLock = new(1, 1);

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetAccessTokenAsync();
            var refreshToken = await GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(refreshToken))
                return false;

            var expStr = await SecureStorage.Default.GetAsync(ExpirationKey);
            bool isExpired = false;
            if (long.TryParse(expStr, out var expUnix))
            {
                var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                isExpired = nowUnix >= expUnix;
            }

            if (isExpired)
            {
                if (string.IsNullOrEmpty(refreshToken))
                    return false;

                try
                {
                    return await RefreshTokenAsync(token);
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException || ex is ApiOfflineException)
                {
                    // Token süresi dolmuş + refresh ağ hatası -> refresh token varsa true dön (çevrimdışı açılış çalışsın)
                    return !string.IsNullOrEmpty(refreshToken);
                }
                catch
                {
                    return false;
                }
            }

            return !string.IsNullOrEmpty(token);
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(AccessTokenKey);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }

        private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
        {
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
            }
            return errorMessage;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Auth/Login", request);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    {
                        return false;
                    }

                    var errorMsg = await ReadErrorMessageAsync(response);
                    throw new ApiException(response.StatusCode, errorMsg);
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result == null || string.IsNullOrEmpty(result.Token))
                    throw new InvalidOperationException("Sunucudan geçerli bir giriş cevabı alınamadı.");

                await SecureStorage.Default.SetAsync(AccessTokenKey, result.Token);
                if (!string.IsNullOrEmpty(result.RefreshToken))
                    await SecureStorage.Default.SetAsync(RefreshTokenKey, result.RefreshToken);

                // Token bitiş zamanını cevaptaki expiration yerine JWT'nin exp claim'inden al
                var expUnix = ExtractJwtExpirationUnixSeconds(result.Token);
                if (expUnix.HasValue)
                {
                    await SecureStorage.Default.SetAsync(ExpirationKey, expUnix.Value.ToString());
                }
                else
                {
                    var fallbackExp = new DateTimeOffset(result.Expiration).ToUnixTimeSeconds();
                    await SecureStorage.Default.SetAsync(ExpirationKey, fallbackExp.ToString());
                }

                return true;
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException)
            {
                throw new ApiOfflineException("Giriş yapılırken sunucuya ulaşılamadı.", ex);
            }
        }

        public async Task<bool> RefreshTokenAsync(string? currentAccessToken = null)
        {
            await _refreshLock.WaitAsync();
            try
            {
                var latestToken = await GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(currentAccessToken) && !string.IsNullOrEmpty(latestToken) && latestToken != currentAccessToken)
                {
                    return true;
                }

                var accessToken = latestToken;
                var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    return false;

                var request = new RefreshTokenRequest
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.PostAsJsonAsync($"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Auth/RefreshToken", request);
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException)
                {
                    // Refresh ağ hatasıyla başarısız olursa çıkış yapma, oturumu düşürme
                    throw new ApiOfflineException("Token yenilenirken sunucuya ulaşılamadı.", ex);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest || 
                    response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Sadece refresh ucu 400/401 dönerse LogoutAsync
                    await LogoutAsync();
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
                if (result == null || string.IsNullOrEmpty(result.AccessToken))
                {
                    await LogoutAsync();
                    return false;
                }

                await SecureStorage.Default.SetAsync(AccessTokenKey, result.AccessToken);
                if (!string.IsNullOrEmpty(result.RefreshToken))
                    await SecureStorage.Default.SetAsync(RefreshTokenKey, result.RefreshToken);

                // Token bitiş zamanını JWT'nin exp claim'inden al
                var expUnix = ExtractJwtExpirationUnixSeconds(result.AccessToken);
                if (expUnix.HasValue)
                {
                    await SecureStorage.Default.SetAsync(ExpirationKey, expUnix.Value.ToString());
                }

                return true;
            }
            finally
            {
                _refreshLock.Release();
            }
        }

        public Task LogoutAsync()
        {
            SecureStorage.Default.Remove(AccessTokenKey);
            SecureStorage.Default.Remove(RefreshTokenKey);
            SecureStorage.Default.Remove(ExpirationKey);
            return Task.CompletedTask;
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return null;

            try
            {
                var parts = token.Split('.');
                if (parts.Length < 2)
                    return null;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(payload);
                using var doc = JsonDocument.Parse(jsonBytes);
                var root = doc.RootElement;

                string email = "";
                if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress", out var ep))
                    email = ep.GetString() ?? "";
                else if (root.TryGetProperty("email", out var ep2))
                    email = ep2.GetString() ?? "";

                string name = "";
                if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", out var np))
                    name = np.GetString() ?? "";
                else if (root.TryGetProperty("name", out var np2))
                    name = np2.GetString() ?? "";
                else if (root.TryGetProperty("unique_name", out var np3))
                    name = np3.GetString() ?? "";

                string role = "";
                if (root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var rp))
                    role = rp.GetString() ?? "";
                else if (root.TryGetProperty("role", out var rp2))
                    role = rp2.GetString() ?? "";

                if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email))
                    name = email.Split('@')[0];

                return new UserInfo
                {
                    Email = email,
                    Name = name,
                    Role = role
                };
            }
            catch
            {
                return null;
            }
        }

        private static long? ExtractJwtExpirationUnixSeconds(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');
                if (parts.Length < 2)
                    return null;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                switch (payload.Length % 4)
                {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                var jsonBytes = Convert.FromBase64String(payload);
                using var doc = JsonDocument.Parse(jsonBytes);
                if (doc.RootElement.TryGetProperty("exp", out var expProp))
                {
                    if (expProp.TryGetInt64(out var expUnix))
                        return expUnix;
                }
            }
            catch
            {
                // Parse edilemezse null
            }
            return null;
        }
    }
}
