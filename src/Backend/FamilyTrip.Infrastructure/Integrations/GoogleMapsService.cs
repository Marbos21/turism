using System.Globalization;
using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class GoogleMapsService : IMapsService
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleMapsService> _logger;

    private static readonly IReadOnlyDictionary<string, string> KindToPlaceType = new Dictionary<string, string>
    {
        ["family_attractions"] = "tourist_attraction",
        ["restaurants"] = "restaurant",
        ["parks"] = "park",
        ["museums"] = "museum",
        ["beaches"] = "natural_feature",
        ["theme_parks"] = "amusement_park"
    };

    public GoogleMapsService(
        HttpClient http,
        IOptions<IntegrationsOptions> options,
        IMemoryCache cache,
        ILogger<GoogleMapsService> logger)
    {
        _http = http;
        _options = options.Value.GoogleMaps;
        _cache = cache;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<PointOfInterest>> GetNearbyPointsAsync(
        double latitude, double longitude, string kind, int limit, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return Array.Empty<PointOfInterest>();

        var placeType = KindToPlaceType.TryGetValue(kind.ToLowerInvariant(), out var t) ? t : "tourist_attraction";
        var cacheKey = $"places:{latitude:F2},{longitude:F2}:{placeType}";
        if (_cache.TryGetValue<List<PointOfInterest>>(cacheKey, out var cached) && cached is not null)
            return cached.Take(limit).ToList();

        var url = string.Create(CultureInfo.InvariantCulture,
            $"maps/api/place/nearbysearch/json?location={latitude},{longitude}&radius=15000&type={placeType}&key={_options.ApiKey}");

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Google Places {Status} for ({Lat},{Lon}) type={Type}", (int)response.StatusCode, latitude, longitude, placeType);
                return Array.Empty<PointOfInterest>();
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var s) ? s.GetString() : null;
            if (status is not "OK" and not "ZERO_RESULTS")
            {
                _logger.LogWarning("Google Places returned status {Status}", status);
                return Array.Empty<PointOfInterest>();
            }

            var results = new List<PointOfInterest>();
            if (root.TryGetProperty("results", out var arr))
            {
                foreach (var r in arr.EnumerateArray())
                {
                    var name = r.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var rating = r.TryGetProperty("rating", out var rt) ? (double?)rt.GetDouble() : null;
                    var total = r.TryGetProperty("user_ratings_total", out var ut) ? (int?)ut.GetInt32() : null;
                    var address = r.TryGetProperty("vicinity", out var v) ? v.GetString() : null;
                    var category = r.TryGetProperty("types", out var types) && types.GetArrayLength() > 0
                        ? types[0].GetString()
                        : null;
                    if (!string.IsNullOrEmpty(name))
                        results.Add(new PointOfInterest(name, category, rating, total, address));
                }
            }

            _cache.Set(cacheKey, results, TimeSpan.FromDays(_options.CacheTtlDays));
            return results.Take(limit).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Google Places call failed");
            return Array.Empty<PointOfInterest>();
        }
    }
}
