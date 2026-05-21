using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class DuffelFlightService : IFlightService
{
    private readonly HttpClient _http;
    private readonly DuffelOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DuffelFlightService> _logger;

    public DuffelFlightService(
        HttpClient http,
        IOptions<IntegrationsOptions> options,
        IMemoryCache cache,
        ILogger<DuffelFlightService> logger)
    {
        _http = http;
        _options = options.Value.Duffel;
        _cache = cache;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<FlightOffer?> GetCheapestOneWayAsync(
        string originIata, string destinationIata,
        DateTime departureDate, int adults, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return null;
        if (string.IsNullOrWhiteSpace(originIata) || string.IsNullOrWhiteSpace(destinationIata)) return null;
        if (string.Equals(originIata, destinationIata, StringComparison.OrdinalIgnoreCase)) return null;

        var cacheKey = $"flight:{originIata}:{destinationIata}:{departureDate:yyyyMMdd}:{adults}";
        if (_cache.TryGetValue<FlightOffer?>(cacheKey, out var cached)) return cached;

        var body = new
        {
            data = new
            {
                slices = new[]
                {
                    new
                    {
                        origin = originIata,
                        destination = destinationIata,
                        departure_date = departureDate.ToString("yyyy-MM-dd")
                    }
                },
                passengers = Enumerable.Range(0, Math.Max(1, adults)).Select(_ => new { type = "adult" }).ToArray(),
                cabin_class = "economy"
            }
        };

        FlightOffer? offer = null;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "air/offer_requests?return_offers=true")
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
                _logger.LogWarning("Duffel flights {Status} {Origin}->{Dest}: {Body}",
                    (int)response.StatusCode, originIata, destinationIata, errorBody);
            }
            else
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                offer = ParseCheapest(doc.RootElement);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Duffel flights call failed {Origin}->{Dest}", originIata, destinationIata);
        }

        // Cache positive AND negative results to avoid hammering the API for unreachable routes.
        _cache.Set(cacheKey, offer, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
        return offer;
    }

    private static FlightOffer? ParseCheapest(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data)) return null;
        if (!data.TryGetProperty("offers", out var offers) || offers.ValueKind != JsonValueKind.Array) return null;

        FlightOffer? best = null;
        foreach (var offer in offers.EnumerateArray())
        {
            try
            {
                if (!offer.TryGetProperty("total_amount", out var amountEl)) continue;
                if (!decimal.TryParse(amountEl.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amount))
                    continue;
                if (best is not null && amount >= best.TotalAmount) continue;

                var currency = offer.TryGetProperty("total_currency", out var cur) ? cur.GetString() ?? "" : "";
                var airline = offer.TryGetProperty("owner", out var owner) && owner.TryGetProperty("name", out var on)
                    ? on.GetString() ?? "" : "";

                if (!offer.TryGetProperty("slices", out var slices) || slices.GetArrayLength() == 0) continue;
                var firstSlice = slices[0];
                if (!firstSlice.TryGetProperty("segments", out var segments) || segments.GetArrayLength() == 0) continue;
                var firstSeg = segments[0];
                var lastSeg = segments[segments.GetArrayLength() - 1];

                var origin = firstSeg.GetProperty("origin").GetProperty("iata_code").GetString() ?? "";
                var dest = lastSeg.GetProperty("destination").GetProperty("iata_code").GetString() ?? "";
                var depart = firstSeg.GetProperty("departing_at").GetDateTime();
                var arrive = lastSeg.GetProperty("arriving_at").GetDateTime();

                best = new FlightOffer(airline, origin, dest, depart, arrive, amount, currency);
            }
            catch
            {
                // Skip malformed offers
            }
        }
        return best;
    }
}
