namespace FamilyTrip.Application.DTOs.Family;

public class FamilyMemberDto
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public int Age { get; set; }
    public string? Interests { get; set; }
    public string? SpecialNeeds { get; set; }
}
