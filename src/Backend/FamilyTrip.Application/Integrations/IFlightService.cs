namespace FamilyTrip.Application.Integrations;

public interface IFlightService
{
    bool IsConfigured { get; }

    Task<FlightOffer?> GetCheapestOneWayAsync(
        string originIata,
        string destinationIata,
        DateTime departureDate,
        int adults,
        CancellationToken cancellationToken = default);
}

public interface IIataResolver
{
    Task<string?> ResolveAsync(string cityOrCountry, CancellationToken cancellationToken = default);
}

public interface ITripSupplementsService
{
    Task<TripSupplements> ResolveAsync(
        DateTime startDate,
        DateTime endDate,
        double destinationLatitude,
        double destinationLongitude,
        string destinationName,
        string destinationCountry,
        int adults,
        CancellationToken cancellationToken = default);
}
