using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class RestCountriesService : ICountryFactsService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RestCountriesService> _logger;

    public RestCountriesService(HttpClient http, IMemoryCache cache, ILogger<RestCountriesService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<CountryFacts?> GetAsync(string countryName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countryName)) return null;
        var key = $"country:{countryName.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue<CountryFacts?>(key, out var cached)) return cached;

        var encoded = Uri.EscapeDataString(countryName.Trim());
        var url = $"v3.1/name/{encoded}?fullText=false&fields=name,capital,languages,currencies,subregion,region";

        CountryFacts? facts = null;
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                {
                    facts = Parse(doc.RootElement[0], countryName);
                }
            }
            else
            {
                _logger.LogDebug("REST Countries {Status} for {Country}", (int)response.StatusCode, countryName);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "REST Countries lookup failed for {Country}", countryName);
        }

        _cache.Set(key, facts, TimeSpan.FromDays(30));
        return facts;
    }

    private static CountryFacts Parse(JsonElement el, string fallbackName)
    {
        var name = el.TryGetProperty("name", out var nameEl) &&
                   nameEl.TryGetProperty("common", out var common)
            ? common.GetString() ?? fallbackName
            : fallbackName;

        string? capital = null;
        if (el.TryGetProperty("capital", out var capArr) && capArr.ValueKind == JsonValueKind.Array && capArr.GetArrayLength() > 0)
            capital = capArr[0].GetString();

        var languages = new List<string>();
        if (el.TryGetProperty("languages", out var langs) && langs.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in langs.EnumerateObject())
            {
                var lang = prop.Value.GetString();
                if (!string.IsNullOrWhiteSpace(lang)) languages.Add(lang);
            }
        }

        string? currency = null;
        if (el.TryGetProperty("currencies", out var currs) && currs.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in currs.EnumerateObject())
            {
                currency = prop.Name;
                break;
            }
        }

        var region = el.TryGetProperty("subregion", out var sub) ? sub.GetString() : null;
        if (string.IsNullOrWhiteSpace(region) && el.TryGetProperty("region", out var r))
            region = r.GetString();

        return new CountryFacts(name, region, capital, languages, currency);
    }
}
