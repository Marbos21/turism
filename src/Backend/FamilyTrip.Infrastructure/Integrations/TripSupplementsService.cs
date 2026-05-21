using FamilyTrip.Application.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.Integrations;

public sealed class TripSupplementsService : ITripSupplementsService
{
    private readonly IFlightService _flights;
    private readonly IHotelService _hotels;
    private readonly IIataResolver _iata;
    private readonly DuffelOptions _options;
    private readonly ILogger<TripSupplementsService> _logger;

    public TripSupplementsService(
        IFlightService flights,
        IHotelService hotels,
        IIataResolver iata,
        IOptions<IntegrationsOptions> options,
        ILogger<TripSupplementsService> logger)
    {
        _flights = flights;
        _hotels = hotels;
        _iata = iata;
        _options = options.Value.Duffel;
        _logger = logger;
    }

    public async Task<TripSupplements> ResolveAsync(
        DateTime startDate, DateTime endDate,
        double destinationLatitude, double destinationLongitude,
        string destinationName, string destinationCountry,
        int adults, CancellationToken cancellationToken = default)
    {
        var adultsClamped = Math.Max(1, adults);

        // Resolve destination IATA. Try the city name, fall back to country.
        var destIata = await _iata.ResolveAsync(destinationName, cancellationToken)
                       ?? await _iata.ResolveAsync(destinationCountry, cancellationToken);
        var originIata = _options.DefaultOriginIata;

        Task<FlightOffer?> outboundTask = Task.FromResult<FlightOffer?>(null);
        Task<FlightOffer?> returnTask = Task.FromResult<FlightOffer?>(null);
        if (!string.IsNullOrWhiteSpace(destIata))
        {
            outboundTask = _flights.GetCheapestOneWayAsync(originIata, destIata!, startDate, adultsClamped, cancellationToken);
            returnTask = _flights.GetCheapestOneWayAsync(destIata!, originIata, endDate, adultsClamped, cancellationToken);
        }
        else
        {
            _logger.LogInformation("No IATA resolved for {City} / {Country} - skipping flight lookup.", destinationName, destinationCountry);
        }

        var hotelTask = _hotels.GetCheapestStayAsync(destinationLatitude, destinationLongitude, startDate, endDate, adultsClamped, cancellationToken);

        await Task.WhenAll(outboundTask, returnTask, hotelTask);

        return new TripSupplements(
            Outbound: outboundTask.Result,
            Return: returnTask.Result,
            Hotels: hotelTask.Result);
    }
}
