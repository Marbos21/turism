using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class DuffelService : IHotelService
{
    private readonly HttpClient _http;
    private readonly DuffelOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DuffelService> _logger;

    public DuffelService(
        HttpClient http,
        IOptions<IntegrationsOptions> options,
        IMemoryCache cache,
        ILogger<DuffelService> logger)
    {
        _http = http;
        _options = options.Value.Duffel;
        _cache = cache;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public Task<HotelAvailability?> GetAvailabilityAsync(
        double latitude, double longitude, int radiusKm, CancellationToken cancellationToken = default)
    {
        // Fixed near-future window so a single lat/lng caches well across requests.
        var checkIn = DateTime.UtcNow.Date.AddDays(30);
        var checkOut = checkIn.AddDays(3);
        var cacheKey = $"hotels:{latitude:F2},{longitude:F2}:{radiusKm}";
        return SearchAsync(latitude, longitude, radiusKm, checkIn, checkOut, adults: 1, cacheKey, cancellationToken);
    }

    public Task<HotelAvailability?> GetCheapestStayAsync(
        double latitude, double longitude,
        DateTime checkIn, DateTime checkOut,
        int adults,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"hotels:stay:{latitude:F2},{longitude:F2}:{checkIn:yyyyMMdd}:{checkOut:yyyyMMdd}:{adults}";
        return SearchAsync(latitude, longitude, radiusKm: 10, checkIn, checkOut, Math.Max(1, adults), cacheKey, cancellationToken);
    }

    private async Task<HotelAvailability?> SearchAsync(
        double latitude, double longitude, int radiusKm,
        DateTime checkIn, DateTime checkOut, int adults,
        string cacheKey, CancellationToken cancellationToken)
    {
        if (!IsConfigured) return null;
        if (_cache.TryGetValue<HotelAvailability>(cacheKey, out var cached) && cached is not null) return cached;

        var body = new
        {
            data = new
            {
                check_in_date = checkIn.ToString("yyyy-MM-dd"),
                check_out_date = checkOut.ToString("yyyy-MM-dd"),
                rooms = 1,
                guests = Enumerable.Range(0, adults).Select(_ => new { type = "adult" }).ToArray(),
                location = new
                {
                    radius = radiusKm,
                    geographic_coordinates = new { latitude, longitude }
                }
            }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "stays/search")
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Headers.Add("Duffel-Version", _options.ApiVersion);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Duffel stays {Status} for ({Lat},{Lon}): {Body}",
                    (int)response.StatusCode, latitude, longitude, errorBody);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var availability = Parse(doc.RootElement, checkIn, checkOut);
            _cache.Set(cacheKey, availability, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
            return availability;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Duffel stays call failed");
            return null;
        }
    }

    private static HotelAvailability Parse(JsonElement root, DateTime checkIn, DateTime checkOut)
    {
        var samples = new List<HotelSample>();
        var total = 0;
        decimal? cheapestTotal = null;
        string? cheapestCurrency = null;
        string? cheapestName = null;

        if (root.TryGetProperty("data", out var data) &&
            data.TryGetProperty("results", out var results) &&
            results.ValueKind == JsonValueKind.Array)
        {
            foreach (var result in results.EnumerateArray())
            {
                total++;

                string accName = "";
                double? review = null;
                string? city = null;

                if (result.TryGetProperty("accommodation", out var acc))
                {
                    accName = acc.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    review = acc.TryGetProperty("review_score", out var rs) && rs.ValueKind == JsonValueKind.Number
                        ? (double?)rs.GetDouble() : null;
                    city = acc.TryGetProperty("location", out var loc) &&
                           loc.TryGetProperty("address", out var addr) &&
                           addr.TryGetProperty("city_name", out var cityEl)
                        ? cityEl.GetString() : null;
                }

                if (samples.Count < 5 && !string.IsNullOrEmpty(accName))
                    samples.Add(new HotelSample(accName, review, city));

                if (result.TryGetProperty("cheapest_rate_total_amount", out var amount) &&
                    decimal.TryParse(amount.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amt))
                {
                    if (cheapestTotal is null || amt < cheapestTotal)
                    {
                        cheapestTotal = amt;
                        cheapestCurrency = result.TryGetProperty("cheapest_rate_total_currency", out var c) ? c.GetString() : null;
                        cheapestName = accName;
                    }
                }
            }
        }

        var nights = Math.Max(1, (int)(checkOut - checkIn).TotalDays);
        decimal? nightly = cheapestTotal is null ? null : Math.Round(cheapestTotal.Value / nights, 2);

        return new HotelAvailability(
            HotelsFound: total,
            SampleHotels: samples,
            CheapestNightlyAmount: nightly,
            Currency: cheapestCurrency,
            CheapestHotelName: cheapestName);
    }
}
