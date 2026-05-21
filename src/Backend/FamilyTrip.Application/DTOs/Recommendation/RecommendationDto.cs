using FamilyTrip.Application.DTOs.Destination;

namespace FamilyTrip.Application.DTOs.Recommendation;

public class RecommendationDto
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public Guid DestinationId { get; set; }
    public DestinationDto? Destination { get; set; }
    public double Score { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
