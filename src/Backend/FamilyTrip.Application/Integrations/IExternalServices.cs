namespace FamilyTrip.Application.Integrations;

public interface IWeatherService
{
    bool IsConfigured { get; }
    Task<WeatherSnapshot?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}

public interface IMapsService
{
    bool IsConfigured { get; }
    Task<IReadOnlyList<PointOfInterest>> GetNearbyPointsAsync(
        double latitude, double longitude, string kind, int limit, CancellationToken cancellationToken = default);
}

public interface IHotelService
{
    bool IsConfigured { get; }

    Task<HotelAvailability?> GetAvailabilityAsync(
        double latitude, double longitude, int radiusKm, CancellationToken cancellationToken = default);

    Task<HotelAvailability?> GetCheapestStayAsync(
        double latitude, double longitude,
        DateTime checkIn, DateTime checkOut,
        int adults,
        CancellationToken cancellationToken = default);
}
