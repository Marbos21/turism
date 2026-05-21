using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Services;

public record ScoredDestination(Destination Destination, double Score, string Reasoning);

public interface IRecommendationEngine
{
    Task<List<ScoredDestination>> ScoreAsync(
        Family family,
        List<FamilyMember> members,
        List<Destination> destinations,
        CancellationToken cancellationToken = default);
}
