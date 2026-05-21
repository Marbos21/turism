using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Application.DTOs.Family;

public class UpdateFamilyRequest
{
    public string Name { get; set; } = string.Empty;
    public BudgetLevel BudgetLevel { get; set; }
    public Climate PreferredClimate { get; set; }
    public TravelStyle TravelStyle { get; set; }
}
