using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Domain.Entities;

public class Destination : BaseEntity
{
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

    public ICollection<TripPlan> TripPlans { get; set; } = new List<TripPlan>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}
