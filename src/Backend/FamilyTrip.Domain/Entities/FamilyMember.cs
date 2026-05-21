namespace FamilyTrip.Domain.Entities;

public class FamilyMember : BaseEntity
{
    public Guid FamilyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string? Interests { get; set; }
    public string? SpecialNeeds { get; set; }

    public Family Family { get; set; } = null!;
}
