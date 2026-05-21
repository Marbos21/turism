using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Services;

public interface IItineraryOptimizer
{
    Task<List<ItineraryItem>> GenerateItineraryAsync(
        TripPlan plan,
        Destination destination,
        Family family,
        List<FamilyMember> members,
        CancellationToken cancellationToken = default);
}
