namespace FamilyTrip.Application.DTOs.TripPlan;

public class CreateTripPlanRequest
{
    public Guid FamilyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid DestinationId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal EstimatedBudget { get; set; }
}
