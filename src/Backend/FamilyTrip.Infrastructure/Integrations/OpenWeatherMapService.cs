using System.Globalization;
using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class OpenWeatherMapService : IWeatherService
{
    private readonly HttpClient _http;
    private readonly OpenWeatherMapOptions _options;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenWeatherMapService> _logger;

    public OpenWeatherMapService(
        HttpClient http,
        IOptions<IntegrationsOptions> options,
        IMemoryCache cache,
        ILogger<OpenWeatherMapService> logger)
    {
        _http = http;
        _options = options.Value.OpenWeatherMap;
        _cache = cache;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<WeatherSnapshot?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured) return null;

        var cacheKey = $"weather:{latitude:F2},{longitude:F2}";
        if (_cache.TryGetValue<WeatherSnapshot>(cacheKey, out var cached) && cached is not null) return cached;

        var url = string.Create(CultureInfo.InvariantCulture,
            $"data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&appid={_options.ApiKey}");

        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenWeatherMap {Status} for ({Lat},{Lon})", (int)response.StatusCode, latitude, longitude);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var snapshot = ParseForecast(doc.RootElement);
            _cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(_options.CacheTtlMinutes));
            return snapshot;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "OpenWeatherMap call failed for ({Lat},{Lon})", latitude, longitude);
            return null;
        }
    }

    private static WeatherSnapshot ParseForecast(JsonElement root)
    {
        if (!root.TryGetProperty("list", out var list) || list.ValueKind != JsonValueKind.Array)
            return new WeatherSnapshot(0, 0, 0, 0, "no data");

        var temps = new List<double>();
        var rainyDayBuckets = new HashSet<string>();

        foreach (var entry in list.EnumerateArray())
        {
            if (entry.TryGetProperty("main", out var main) && main.TryGetProperty("temp", out var tempEl))
                temps.Add(tempEl.GetDouble());

            if (entry.TryGetProperty("weather", out var weatherArr) && weatherArr.GetArrayLength() > 0)
            {
                var main2 = weatherArr[0].TryGetProperty("main", out var m) ? m.GetString() : null;
                if (main2 is "Rain" or "Drizzle" or "Thunderstorm" or "Snow")
                {
                    var dateTxt = entry.TryGetProperty("dt_txt", out var dt) ? dt.GetString() ?? "" : "";
                    rainyDayBuckets.Add(dateTxt[..Math.Min(10, dateTxt.Length)]);
                }
            }
        }

        if (temps.Count == 0) return new WeatherSnapshot(0, 0, 0, 0, "no data");

        var avg = temps.Average();
        var summary = avg switch
        {
            < 0 => "cold",
            < 10 => "cool",
            < 20 => "mild",
            < 28 => "warm",
            _ => "hot"
        };

        return new WeatherSnapshot(
            AverageTempCelsius: Math.Round(avg, 1),
            MinTempCelsius: Math.Round(temps.Min(), 1),
            MaxTempCelsius: Math.Round(temps.Max(), 1),
            RainyDays: rainyDayBuckets.Count,
            Summary: summary);
    }
}
