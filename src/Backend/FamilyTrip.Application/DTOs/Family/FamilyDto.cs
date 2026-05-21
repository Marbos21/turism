using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Application.DTOs.Family;

public class FamilyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public BudgetLevel BudgetLevel { get; set; }
    public Climate PreferredClimate { get; set; }
    public TravelStyle TravelStyle { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<FamilyMemberDto> Members { get; set; } = new();
}
