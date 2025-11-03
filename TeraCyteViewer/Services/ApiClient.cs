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

        private HttpClient CreateClient()
        {
            var c = _factory.CreateClient("TeraCyte");
            if (!string.IsNullOrEmpty(_auth.AccessToken))
                c.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _auth.AccessToken);
            return c;
        }

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

                    if (triedRefresh)
                        throw new UnauthorizedAccessException("Session expired. Please log in again.", ex);

                    var ok = await _auth.RefreshAsync(ct);
                    if (!ok)
                        throw new UnauthorizedAccessException("Session expired. Please log in again.");

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
                    _log.LogInformation("histogram received :{Histogram}",obj?.histogram );
                    return obj!;
                });
            }, ct);
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";

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
