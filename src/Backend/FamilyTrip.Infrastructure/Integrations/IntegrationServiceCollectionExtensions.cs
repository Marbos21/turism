using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FamilyTrip.Infrastructure.Integrations;

public static class IntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddExternalIntegrations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddOptions<IntegrationsOptions>()
            .Bind(configuration.GetSection(IntegrationsOptions.SectionName))
            .PostConfigure(opts =>
            {
                opts.OpenWeatherMap.ApiKey = NonEmpty(Environment.GetEnvironmentVariable("OPENWEATHER_API_KEY")) ?? opts.OpenWeatherMap.ApiKey;
                opts.GoogleMaps.ApiKey    = NonEmpty(Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY")) ?? opts.GoogleMaps.ApiKey;
                opts.Duffel.ApiKey        = NonEmpty(Environment.GetEnvironmentVariable("DUFFEL_API_KEY")) ?? opts.Duffel.ApiKey;
            });

        services.AddHttpClient<IWeatherService, OpenWeatherMapService>((sp, http) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationsOptions>>().Value.OpenWeatherMap;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<IMapsService, GoogleMapsService>((sp, http) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationsOptions>>().Value.GoogleMaps;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpClient<IHotelService, DuffelService>((sp, http) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationsOptions>>().Value.Duffel;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddHttpClient<IFlightService, DuffelFlightService>((sp, http) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationsOptions>>().Value.Duffel;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IIataResolver, DuffelIataResolver>((sp, http) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IntegrationsOptions>>().Value.Duffel;
            http.BaseAddress = new Uri(opts.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddScoped<ITripSupplementsService, TripSupplementsService>();

        services.AddHttpClient<IWikipediaImageResolver, WikipediaImageResolver>(http =>
        {
            http.BaseAddress = new Uri("https://en.wikipedia.org/");
            http.Timeout = TimeSpan.FromSeconds(8);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("FamilyTrip/1.0 (https://familytrip.app)");
        });

        services.AddHttpClient<ICountryFactsService, RestCountriesService>(http =>
        {
            http.BaseAddress = new Uri("https://restcountries.com/");
            http.Timeout = TimeSpan.FromSeconds(8);
        });

        return services;
    }

    private static string? NonEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
