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
        Task<bool> RefreshTokenAsync();
        Task LogoutAsync();
        Task<UserInfo?> GetCurrentUserAsync();
    }

    public class AuthService : IAuthService
    {
        private const string AccessTokenKey = "StaffSync_AccessToken";
        private const string RefreshTokenKey = "StaffSync_RefreshToken";
        private const string ExpirationKey = "StaffSync_TokenExpiration";

        private readonly HttpClient _httpClient;

        public AuthService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(token))
                return false;

            var expStr = await SecureStorage.Default.GetAsync(ExpirationKey);
            if (DateTime.TryParse(expStr, out var exp))
            {
                // Token süresi dolduysa ve refresh token varsa yenilemeyi dene
                if (DateTime.UtcNow >= exp)
                {
                    return await RefreshTokenAsync();
                }
            }

            return true;
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(AccessTokenKey);
        }

        public async Task<string?> GetRefreshTokenAsync()
        {
            return await SecureStorage.Default.GetAsync(RefreshTokenKey);
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var request = new LoginRequest { Email = email, Password = password };
                var response = await _httpClient.PostAsJsonAsync($"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Auth/Login", request);

                if (!response.IsSuccessStatusCode)
                    return false;

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result == null || string.IsNullOrEmpty(result.Token))
                    return false;

                await SecureStorage.Default.SetAsync(AccessTokenKey, result.Token);
                if (!string.IsNullOrEmpty(result.RefreshToken))
                    await SecureStorage.Default.SetAsync(RefreshTokenKey, result.RefreshToken);

                await SecureStorage.Default.SetAsync(ExpirationKey, result.Expiration.ToString("O"));

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
                var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                    return false;

                var request = new RefreshTokenRequest
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                var response = await _httpClient.PostAsJsonAsync($"{ApiConfig.BaseUrl.TrimEnd('/')}/api/Auth/RefreshToken", request);
                if (!response.IsSuccessStatusCode)
                {
                    await LogoutAsync();
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (result == null || string.IsNullOrEmpty(result.Token))
                {
                    await LogoutAsync();
                    return false;
                }

                await SecureStorage.Default.SetAsync(AccessTokenKey, result.Token);
                if (!string.IsNullOrEmpty(result.RefreshToken))
                    await SecureStorage.Default.SetAsync(RefreshTokenKey, result.RefreshToken);

                await SecureStorage.Default.SetAsync(ExpirationKey, result.Expiration.ToString("O"));
                return true;
            }
            catch
            {
                return false;
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
    }
}
