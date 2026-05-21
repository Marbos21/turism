using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Application.DTOs.Destination;

public class DestinationSearchRequest
{
    public string? SearchTerm { get; set; }
    public string? Country { get; set; }
    public Climate? Climate { get; set; }
    public int? MinChildFriendlyScore { get; set; }
    public decimal? MaxCostPerDay { get; set; }
    public string? Category { get; set; }
}
