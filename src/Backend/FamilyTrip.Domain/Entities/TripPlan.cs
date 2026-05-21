using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Domain.Entities;

public class TripPlan : BaseEntity
{
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid DestinationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal EstimatedBudget { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Draft;

    public Family Family { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
    public ICollection<ItineraryItem> ItineraryItems { get; set; } = new List<ItineraryItem>();
}
