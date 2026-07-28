using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.IntegrationTests.LiveApi;

/// <summary>
/// Shared plumbing for the "LiveIntegration" suite: real HTTP calls against the already-running
/// docker stack (default http://localhost:8080, override via HEK_API_BASE_URL) - not
/// WebApplicationFactory, not Testcontainers. Mirrors exactly how this was verified by hand this
/// session (see frontend/src/api.ts for the same auth contracts).
/// </summary>
internal static class LiveApi
{
    public static HttpClient CreateClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("HEK_API_BASE_URL") ?? "http://localhost:8080";
        return new HttpClient(new RateLimitPacingHandler(new HttpClientHandler())) { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// The real API enforces RateLimit:PermitLimit=30/WindowSeconds=60 (appsettings.json) - a whole
    /// test run's worth of requests fires in a couple of seconds otherwise, well past that limit, and
    /// every request past #30 gets a false-negative 429 that has nothing to do with the endpoint under
    /// test. Serializes every HTTP call across the whole process (assembly-level, not per-client) at a
    /// pace comfortably under the limit (~24/60s) instead.
    /// </summary>
    private sealed class RateLimitPacingHandler : DelegatingHandler
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(2600);
        private static DateTime _lastRequestUtc = DateTime.MinValue;

        public RateLimitPacingHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            await Gate.WaitAsync(ct);
            try
            {
                var elapsedSinceLast = DateTime.UtcNow - _lastRequestUtc;
                if (elapsedSinceLast < MinInterval)
                {
                    await Task.Delay(MinInterval - elapsedSinceLast, ct);
                }

                _lastRequestUtc = DateTime.UtcNow;
                return await base.SendAsync(request, ct);
            }
            finally
            {
                Gate.Release();
            }
        }
    }

    public static async Task EnsureReachableAsync(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync("/karo/ping");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Live API not reachable at {client.BaseAddress} - is `docker compose up -d` running? ({ex.Message})", ex);
        }
    }
}

/// <summary>HISO uses a session GUID, not bearer auth - no login call, just a reachability check.</summary>
public sealed class HisoLiveFixture : IAsyncLifetime
{
    public HttpClient Client { get; } = LiveApi.CreateClient();

    public string SessionKey => KnownTestData.HisoSessionKey;

    public Task InitializeAsync() => LiveApi.EnsureReachableAsync(Client);

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}

/// <summary>Authenticates once (JSON contract, see frontend/src/api.ts:karoAuthenticate) and caches the bearer token for every test in the class.</summary>
public sealed class KaroLiveFixture : IAsyncLifetime
{
    public HttpClient Client { get; } = LiveApi.CreateClient();

    public string Token { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await LiveApi.EnsureReachableAsync(Client);

        var response = await Client.PostAsJsonAsync("/karo/authenticate", new
        {
            username = KnownTestData.KaroUsername,
            password = KnownTestData.KaroPassword,
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
            system = KnownTestData.KaroSystem,
            pho = KnownTestData.KaroPho,
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!body.TryGetProperty("status", out var status) || status.GetString() != "success")
        {
            throw new InvalidOperationException($"KARO authenticate did not succeed: {body}");
        }

        Token = body.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("KARO authenticate succeeded but returned no token.");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}

/// <summary>Authenticates once (XML contract, see frontend/src/api.ts:ermsAuthenticate) and caches the bearer token for every test in the class.</summary>
public sealed class ErmsLiveFixture : IAsyncLifetime
{
    public HttpClient Client { get; } = LiveApi.CreateClient();

    public string Token { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await LiveApi.EnsureReachableAsync(Client);

        var xml = $"<?xml version=\"1.0\"?><Credential><Username>{KnownTestData.ErmsUsername}</Username>" +
                  $"<Password>{KnownTestData.ErmsPassword}</Password><PatientId>{KnownTestData.PatientId}</PatientId>" +
                  $"<EncounterId>{KnownTestData.EncounterId}</EncounterId></Credential>";

        var response = await Client.PostAsync("/erms/authenticate", new StringContent(xml, System.Text.Encoding.UTF8, "text/xml"));
        response.EnsureSuccessStatusCode();

        // Real response is UTF-16 (see frontend/src/api.ts's ermsAuthenticate comment) - decode using
        // the response's own declared charset, not a blind UTF-8 read.
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "";
        var charsetMatch = System.Text.RegularExpressions.Regex.Match(contentType, "charset=([^;]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var charset = charsetMatch.Success ? charsetMatch.Groups[1].Value.Trim() : "utf-8";
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var text = System.Text.Encoding.GetEncoding(charset).GetString(bytes);

        var tokenMatch = System.Text.RegularExpressions.Regex.Match(text, "<Token>([^<]*)</Token>");
        if (!tokenMatch.Success || string.IsNullOrEmpty(tokenMatch.Groups[1].Value))
        {
            throw new InvalidOperationException($"ERMS authenticate did not succeed: {text}");
        }

        Token = tokenMatch.Groups[1].Value;
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}

/// <summary>Authenticates once (JSON contract, see frontend/src/api.ts:colAuthenticate) and caches the bearer token for every test in the class.</summary>
public sealed class ColLiveFixture : IAsyncLifetime
{
    public HttpClient Client { get; } = LiveApi.CreateClient();

    public string Token { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await LiveApi.EnsureReachableAsync(Client);

        var response = await Client.PostAsJsonAsync("/erms/col/authenticate", new
        {
            username = KnownTestData.ColUsername,
            password = KnownTestData.ColPassword,
            patientId = KnownTestData.PatientId,
            encounterId = KnownTestData.EncounterId,
        });
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Token = body.TryGetProperty("Token", out var token) ? token.GetString() ?? string.Empty : string.Empty;
        if (string.IsNullOrEmpty(Token))
        {
            throw new InvalidOperationException($"COL authenticate did not succeed: {body}");
        }

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
