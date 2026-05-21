namespace FamilyTrip.Application.DTOs.Family;

public class AddFamilyMemberRequest
{
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string? Interests { get; set; }
    public string? SpecialNeeds { get; set; }
}
