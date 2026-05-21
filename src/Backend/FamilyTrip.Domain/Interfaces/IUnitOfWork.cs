using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Family> Families { get; }
    IRepository<FamilyMember> FamilyMembers { get; }
    IRepository<Destination> Destinations { get; }
    IRepository<TripPlan> TripPlans { get; }
    IRepository<ItineraryItem> ItineraryItems { get; }
    IRepository<Recommendation> Recommendations { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
