namespace FamilyTrip.Domain.Entities;

public class ItineraryItem : BaseEntity
{
    public Guid TripPlanId { get; set; }
    public int DayNumber { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string ActivityName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Duration { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Notes { get; set; }

    public TripPlan TripPlan { get; set; } = null!;
}
