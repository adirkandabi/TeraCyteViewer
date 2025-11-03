using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System;
using TeraCyteViewer.Models;

namespace TeraCyteViewer.Services
{
    public class ApiClient
    {
        private readonly IHttpClientFactory _factory;
        private readonly AuthService _auth;
        private readonly ILogger<ApiClient> _log;

        // Configure JSON parsing to accept numeric strings and ignore casing differences.
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            PropertyNameCaseInsensitive = true
        };

        public ApiClient(IHttpClientFactory factory, AuthService auth, ILogger<ApiClient> log)
        {
            _factory = factory;
            _auth = auth;
            _log = log;
        }

        // Creates an HttpClient preconfigured with the access token if available.
        private HttpClient CreateClient()
        {
            var c = _factory.CreateClient("TeraCyte");
            if (!string.IsNullOrEmpty(_auth.AccessToken))
                c.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _auth.AccessToken);
            return c;
        }

        // Executes an HTTP call with automatic retry on 401 (token expired) once after refresh.
        private async Task<T> WithAuthRetry<T>(Func<HttpClient, Task<T>> action, CancellationToken ct = default)
        {
            using var client = CreateClient();
            var triedRefresh = false;

            while (true)
            {
                try
                {
                    return await action(client);
                }
                catch (HttpRequestException ex) when (Contains401(ex))
                {
                    _log.LogWarning("401 detected{Suffix}", triedRefresh ? " (after refresh)" : string.Empty);

                    // If we already retried after refresh, force logout.
                    if (triedRefresh)
                        throw new UnauthorizedAccessException("Session expired. Please log in again.", ex);

                    // Try to refresh the token once.
                    var ok = await _auth.RefreshAsync(ct);
                    if (!ok)
                        throw new UnauthorizedAccessException("Session expired. Please log in again.");

                    // Re-attach the new token and retry once.
                    if (!string.IsNullOrEmpty(_auth.AccessToken))
                        client.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _auth.AccessToken);

                    _log.LogInformation("Token refreshed, retrying request...");
                    triedRefresh = true;
                    continue;
                }
            }

            static bool Contains401(HttpRequestException ex)
                => ex.Message.Contains("401");
        }

        // Fetches the latest image data from the API, with retry and logging.
        public async Task<ImageResponse> GetImageAsync(CancellationToken ct = default)
        {
            return await WithAuthRetry(async client =>
            {
                return await WithTransientRetry(async () =>
                {
                    using var resp = await client.GetAsync("api/image", ct);
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    _log.LogInformation("GET /api/image -> {Status} bodyLen={Len}", (int)resp.StatusCode, body.Length);

                    if (!resp.IsSuccessStatusCode)
                        throw new HttpRequestException($"GET /api/image {(int)resp.StatusCode}. Body: {Truncate(body, 200)}");

                    var obj = JsonSerializer.Deserialize<ImageResponse>(body, JsonOpts);
                    _log.LogInformation("Image received id={Id} ts={Ts}", obj?.image_id, obj?.timestamp);
                    return obj!;
                });
            }, ct);
        }

        // Fetches image analysis results, retries transient errors, and logs details.
        public async Task<ResultsResponse> GetResultsAsync(CancellationToken ct = default)
        {
            return await WithAuthRetry(async client =>
            {
                return await WithTransientRetry(async () =>
                {
                    using var resp = await client.GetAsync("api/results", ct);
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    _log.LogInformation("GET /api/results -> {Status} bodyLen={Len}", (int)resp.StatusCode, body.Length);

                    if (!resp.IsSuccessStatusCode)
                        throw new HttpRequestException($"GET /api/results {(int)resp.StatusCode}. Body: {Truncate(body, 200)}");

                    var obj = JsonSerializer.Deserialize<ResultsResponse>(body, JsonOpts);
                    _log.LogInformation("Results received id={Id} avg={Avg} focus={Focus} label={Label}",
                        obj?.image_id, obj?.intensity_average, obj?.focus_score, obj?.classification_label);
                    _log.LogInformation("histogram received :{Histogram}", obj?.histogram);
                    return obj!;
                });
            }, ct);
        }

        // Helper: truncates long responses in logs to avoid noise.
        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

        // Executes an action with retry on transient (5xx) server errors.
        private static async Task<T> WithTransientRetry<T>(Func<Task<T>> action)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    return await action();
                }
                catch (HttpRequestException ex) when (attempt < 2 && IsTransient(ex))
                {
                    attempt++;
                    // Small backoff with jitter before retrying.
                    var delay = TimeSpan.FromMilliseconds(300 * attempt + Random.Shared.Next(0, 200));
                    await Task.Delay(delay);
                }
            }

            static bool IsTransient(HttpRequestException ex)
                => ex.Message.Contains("500") ||
                   ex.Message.Contains("502") ||
                   ex.Message.Contains("503") ||
                   ex.Message.Contains("504");
        }
    }
}
