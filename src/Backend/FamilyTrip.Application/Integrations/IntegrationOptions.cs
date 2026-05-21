namespace FamilyTrip.Application.Integrations;

public class IntegrationsOptions
{
    public const string SectionName = "Integrations";

    public OpenWeatherMapOptions OpenWeatherMap { get; set; } = new();
    public GoogleMapsOptions GoogleMaps { get; set; } = new();
    public DuffelOptions Duffel { get; set; } = new();
}

public class OpenWeatherMapOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.openweathermap.org/";
    public int CacheTtlMinutes { get; set; } = 60;
}

public class GoogleMapsOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://maps.googleapis.com/";
    public int CacheTtlDays { get; set; } = 30;
}

public class DuffelOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.duffel.com/";
    public string ApiVersion { get; set; } = "v2";
    public int CacheTtlMinutes { get; set; } = 15;
    public string DefaultOriginIata { get; set; } = "OTP";  // Bucharest Henri Coanda
}
