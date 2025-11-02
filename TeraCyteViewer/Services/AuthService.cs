using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TeraCyteViewer.Services
{
    public class AuthService
    {
        private readonly IHttpClientFactory _factory;
        private readonly ILogger<AuthService> _log;

        public string? AccessToken { get; private set; }
        public string? RefreshToken { get; private set; }
        public DateTimeOffset ExpiresAtUtc { get; private set; }

        public AuthService(IHttpClientFactory factory, ILogger<AuthService> log)
        {
            _factory = factory;
            _log = log;
        }

        public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
        {
            _log.LogInformation("Login attempt for user {User}", username);
            var client = _factory.CreateClient("TeraCyte");
            using var resp = await client.PostAsJsonAsync("api/auth/login", new { username, password }, ct);
            var ok = resp.IsSuccessStatusCode;
            _log.LogInformation("Login result for {User}: {StatusCode}", username, (int)resp.StatusCode);

            if (!ok) return false;

            var json = await resp.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct);
            if (json is null || string.IsNullOrEmpty(json.access_token))
            {
                _log.LogWarning("Login response parse failed");
                return false;
            }

            AccessToken = json.access_token;
            RefreshToken = json.refresh_token;
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(json.expires_in - 30);

            _log.LogInformation("Access token acquired, expires at {Exp}", ExpiresAtUtc);
            return true;
        }

        public async Task<bool> RefreshAsync(CancellationToken ct = default)
        {
            try
            {
                using var client = _factory.CreateClient("TeraCyte");

                var request = new
                {
                    refresh_token = RefreshToken
                };

                var response = await client.PostAsJsonAsync("api/auth/refresh", request, ct);
                var body = await response.Content.ReadAsStringAsync(ct);
                _log.LogInformation("Refresh token -> {Status}", (int)response.StatusCode);

                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("Token refresh failed: {Body}", body);
                    return false;
                }

                var result = JsonSerializer.Deserialize<LoginResponse>(body);
                if (result == null)
                {
                    _log.LogWarning("Token refresh returned null");
                    return false;
                }

                AccessToken = result.access_token;
                RefreshToken = result.refresh_token;
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(result.expires_in - 30);
                _log.LogInformation("Token refreshed successfully, expires at {Time}", ExpiresAtUtc);

                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Exception during token refresh");
                return false;
            }
        }


        private sealed class LoginResponse
        {
            public string access_token { get; set; } = "";
            public string refresh_token { get; set; } = "";
            public string token_type { get; set; } = "";
            public int expires_in { get; set; }
        }
    }
}
