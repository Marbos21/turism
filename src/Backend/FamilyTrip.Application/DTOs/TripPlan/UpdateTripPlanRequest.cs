using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Application.DTOs.TripPlan;

public class UpdateTripPlanRequest
{
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal EstimatedBudget { get; set; }
    public TripStatus Status { get; set; }
}
