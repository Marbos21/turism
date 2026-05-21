using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Application.DTOs.Destination;

public class DestinationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ChildFriendlyScore { get; set; }
    public decimal AvgCostPerDay { get; set; }
    public Climate Climate { get; set; }
    public string? ImageUrl { get; set; }

    // Enriched per-request from external sources (REST Countries + climate-derived).
    // Optional - null when the lookup is unavailable.
    public string? Region { get; set; }
    public string? Capital { get; set; }
    public List<string> Languages { get; set; } = new();
    public string? Currency { get; set; }
    public List<int> BestMonths { get; set; } = new();
}
