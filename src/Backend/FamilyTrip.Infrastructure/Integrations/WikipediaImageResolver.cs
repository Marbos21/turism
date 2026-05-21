using System.Net.Http.Headers;
using System.Text.Json;
using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class WikipediaImageResolver : IWikipediaImageResolver
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WikipediaImageResolver> _logger;

    public WikipediaImageResolver(HttpClient http, IMemoryCache cache, ILogger<WikipediaImageResolver> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetThumbnailUrlAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var cacheKey = $"wiki:image:{title}";
        if (_cache.TryGetValue<string?>(cacheKey, out var cached)) return cached;

        var encoded = Uri.EscapeDataString(title.Replace(' ', '_'));
        var url = $"api/rest_v1/page/summary/{encoded}";

        string? imageUrl = null;
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

                if (doc.RootElement.TryGetProperty("originalimage", out var original) &&
                    original.TryGetProperty("source", out var origSrc))
                {
                    imageUrl = origSrc.GetString();
                }
                else if (doc.RootElement.TryGetProperty("thumbnail", out var thumb) &&
                         thumb.TryGetProperty("source", out var thumbSrc))
                {
                    imageUrl = thumbSrc.GetString();
                }
            }
            else
            {
                _logger.LogDebug("Wikipedia summary {Status} for {Title}", (int)response.StatusCode, title);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Wikipedia lookup failed for {Title}", title);
        }

        // Cache both hits and misses (null = "we tried, no image") to avoid repeating failures.
        _cache.Set(cacheKey, imageUrl, TimeSpan.FromDays(30));
        return imageUrl;
    }
}
