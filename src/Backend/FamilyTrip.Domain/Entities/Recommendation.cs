namespace FamilyTrip.Domain.Entities;

public class Recommendation : BaseEntity
{
    public Guid FamilyId { get; set; }
    public Guid DestinationId { get; set; }
    public double Score { get; set; }
    public string Reasoning { get; set; } = string.Empty;

    public Family Family { get; set; } = null!;
    public Destination Destination { get; set; } = null!;
}
