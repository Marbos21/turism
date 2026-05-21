using FamilyTrip.Application.DTOs.TripPlan;

namespace FamilyTrip.Application.Services;

public interface ITripPlanAppService
{
    Task<List<TripPlanDto>> GetByFamilyAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default);
    Task<TripPlanDto> CreateAsync(Guid userId, CreateTripPlanRequest request, CancellationToken cancellationToken = default);
    Task<TripPlanDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<TripPlanDto> UpdateAsync(Guid id, Guid userId, UpdateTripPlanRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<List<ItineraryItemDto>> GenerateItineraryAsync(Guid tripPlanId, Guid userId, CancellationToken cancellationToken = default);
}
