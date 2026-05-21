using FamilyTrip.Application.DTOs.Destination;
using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Application.DTOs.TripPlan;

public class TripPlanDto
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid DestinationId { get; set; }
    public DestinationDto? Destination { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal EstimatedBudget { get; set; }
    public TripStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ItineraryItemDto> ItineraryItems { get; set; } = new();
}
