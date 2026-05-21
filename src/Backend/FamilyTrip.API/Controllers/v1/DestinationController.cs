using Asp.Versioning;
using FamilyTrip.Application.DTOs.Destination;
using FamilyTrip.Application.Integrations;
using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FamilyTrip.API.Controllers.v1;

/// <summary>
/// Provides destination search and discovery.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/destinations")]
public class DestinationController : ControllerBase
{
    private readonly IDestinationAppService _destinationService;
    private readonly IWeatherService _weather;
    private readonly IMapsService _maps;
    private readonly IHotelService _hotels;
    private readonly ICountryFactsService _countryFacts;

    public DestinationController(
        IDestinationAppService destinationService,
        IWeatherService weather,
        IMapsService maps,
        IHotelService hotels,
        ICountryFactsService countryFacts)
    {
        _destinationService = destinationService;
        _weather = weather;
        _maps = maps;
        _hotels = hotels;
        _countryFacts = countryFacts;
    }

    private static readonly Dictionary<Climate, int[]> BestMonthsByClimate = new()
    {
        // Climate enum: Tropical=1, Mediterranean=2, Continental=3, Arctic=4
        [Climate.Tropical]      = new[] { 11, 12, 1, 2, 3, 4 },   // dry season
        [Climate.Mediterranean] = new[] { 4, 5, 6, 9, 10 },        // warm but not peak heat
        [Climate.Continental]   = new[] { 5, 6, 7, 8, 9 },         // late spring to early autumn
        [Climate.Arctic]        = new[] { 6, 7, 8 }                // brief summer window
    };

    private async Task EnrichAsync(DestinationDto dto, CancellationToken cancellationToken)
    {
        var facts = await _countryFacts.GetAsync(dto.Country, cancellationToken);
        if (facts is not null)
        {
            dto.Region = facts.Region;
            dto.Capital = facts.Capital;
            dto.Languages = facts.Languages.ToList();
            dto.Currency = facts.Currency;
        }
        if (BestMonthsByClimate.TryGetValue(dto.Climate, out var months))
            dto.BestMonths = months.ToList();
    }

    /// <summary>
    /// Searches and filters destinations.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DestinationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? searchTerm,
        [FromQuery] string? country,
        [FromQuery] Climate? climate,
        [FromQuery] int? minChildFriendlyScore,
        [FromQuery] decimal? maxCostPerDay,
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        var request = new DestinationSearchRequest
        {
            SearchTerm = searchTerm,
            Country = country,
            Climate = climate,
            MinChildFriendlyScore = minChildFriendlyScore,
            MaxCostPerDay = maxCostPerDay,
            Category = category
        };

        var destinations = await _destinationService.SearchAsync(request, cancellationToken);
        return Ok(destinations);
    }

    /// <summary>
    /// Returns the most popular family-friendly destinations.
    /// </summary>
    [HttpGet("popular")]
    [ProducesResponseType(typeof(List<DestinationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPopular(CancellationToken cancellationToken)
    {
        var destinations = await _destinationService.GetPopularAsync(cancellationToken);
        return Ok(destinations);
    }

    /// <summary>
    /// Returns a destination by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DestinationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var destination = await _destinationService.GetByIdAsync(id, cancellationToken);
            await EnrichAsync(destination, cancellationToken);
            return Ok(destination);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Returns the destination bundled with live data from external sources
    /// (weather, points of interest, hotel availability). Any source that is
    /// not configured or returns no data is omitted from the response.
    /// </summary>
    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(DestinationDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(Guid id, CancellationToken cancellationToken)
    {
        DestinationDto destination;
        try
        {
            destination = await _destinationService.GetByIdAsync(id, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Error = ex.Message });
        }

        var enrichTask = EnrichAsync(destination, cancellationToken);
        var weatherTask = _weather.GetForecastAsync(destination.Latitude, destination.Longitude, cancellationToken);
        var placesTask = _maps.GetNearbyPointsAsync(destination.Latitude, destination.Longitude, "family_attractions", 8, cancellationToken);
        var hotelsTask = _hotels.GetAvailabilityAsync(destination.Latitude, destination.Longitude, 10, cancellationToken);

        await Task.WhenAll(enrichTask, weatherTask, placesTask, hotelsTask);

        var weather = weatherTask.Result;
        var places = placesTask.Result;
        var hotels = hotelsTask.Result;

        return Ok(new DestinationDetailsResponse
        {
            Destination = destination,
            Weather = weather is null ? null : new WeatherSnapshotDto
            {
                AverageTempCelsius = weather.AverageTempCelsius,
                MinTempCelsius = weather.MinTempCelsius,
                MaxTempCelsius = weather.MaxTempCelsius,
                RainyDays = weather.RainyDays,
                Summary = weather.Summary
            },
            Attractions = places.Select(p => new PointOfInterestDto
            {
                Name = p.Name,
                Category = p.Category,
                Rating = p.Rating,
                UserRatingsTotal = p.UserRatingsTotal,
                Address = p.Address
            }).ToList(),
            Hotels = hotels is null ? null : new HotelAvailabilityDto
            {
                HotelsFound = hotels.HotelsFound,
                SampleHotels = hotels.SampleHotels.Select(h => new HotelSampleDto
                {
                    Name = h.Name,
                    Rating = h.Rating,
                    Address = h.Address
                }).ToList()
            }
        });
    }
}
