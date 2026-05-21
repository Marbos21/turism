namespace FamilyTrip.Application.Integrations;

public sealed record WeatherSnapshot(
    double AverageTempCelsius,
    double MinTempCelsius,
    double MaxTempCelsius,
    int RainyDays,
    string Summary);

public sealed record PointOfInterest(
    string Name,
    string? Category,
    double? Rating,
    int? UserRatingsTotal,
    string? Address);

public sealed record HotelAvailability(
    int HotelsFound,
    IReadOnlyList<HotelSample> SampleHotels,
    decimal? CheapestNightlyAmount = null,
    string? Currency = null,
    string? CheapestHotelName = null);

public sealed record HotelSample(string Name, double? Rating, string? Address);

public sealed record FlightOffer(
    string Airline,
    string OriginIata,
    string DestinationIata,
    DateTime DepartureAt,
    DateTime ArrivalAt,
    decimal TotalAmount,
    string Currency);

public sealed record TripSupplements(
    FlightOffer? Outbound,
    FlightOffer? Return,
    HotelAvailability? Hotels);
