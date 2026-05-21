using System.Net.Http.Headers;
using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class DuffelIataResolver : IIataResolver
{
    private readonly HttpClient _http;
    private readonly DuffelOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DuffelIataResolver> _logger;

    public DuffelIataResolver(
        HttpClient http,
        IOptions<IntegrationsOptions> options,
        IMemoryCache cache,
        ILogger<DuffelIataResolver> logger)
    {
        _http = http;
        _options = options.Value.Duffel;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> ResolveAsync(string cityOrCountry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityOrCountry) || string.IsNullOrWhiteSpace(_options.ApiKey)) return null;
        var cacheKey = $"iata:{cityOrCountry.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue<string?>(cacheKey, out var cached)) return cached;

        var query = Uri.EscapeDataString(cityOrCountry.Trim());
        string? iata = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"places/suggestions?query={query}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Add("Duffel-Version", _options.ApiVersion);
            using var response = await _http.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (doc.RootElement.TryGetProperty("data", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    // Prefer a "city" with iata_code; fall back to first iata_code we find.
                    foreach (var item in arr.EnumerateArray())
                    {
                        var type = item.TryGetProperty("type", out var t) ? t.GetString() : null;
                        var code = item.TryGetProperty("iata_code", out var c) ? c.GetString() : null;
                        if (type == "city" && !string.IsNullOrEmpty(code)) { iata = code; break; }
                        if (iata is null && !string.IsNullOrEmpty(code)) iata = code;
                    }
                }
            }
            else
            {
                _logger.LogDebug("Duffel places {Status} for {Query}", (int)response.StatusCode, cityOrCountry);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Duffel places lookup failed for {Query}", cityOrCountry);
        }

        _cache.Set(cacheKey, iata, TimeSpan.FromDays(30));
        return iata;
    }
}
