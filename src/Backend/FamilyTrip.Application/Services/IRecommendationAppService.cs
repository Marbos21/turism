using FamilyTrip.Application.DTOs.Recommendation;

namespace FamilyTrip.Application.Services;

public interface IRecommendationAppService
{
    Task<GenerateRecommendationsResponse> GenerateAsync(GenerateRecommendationsRequest request, Guid userId, CancellationToken cancellationToken = default);
    Task<List<RecommendationDto>> GetByFamilyAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default);
}
