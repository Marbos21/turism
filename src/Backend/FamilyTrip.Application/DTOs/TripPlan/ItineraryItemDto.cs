namespace FamilyTrip.Application.DTOs.TripPlan;

public class ItineraryItemDto
{
    public Guid Id { get; set; }
    public Guid TripPlanId { get; set; }
    public int DayNumber { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public string ActivityName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Duration { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Notes { get; set; }
}
