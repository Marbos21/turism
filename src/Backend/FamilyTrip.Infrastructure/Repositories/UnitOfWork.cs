using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Interfaces;
using FamilyTrip.Infrastructure.Data;

namespace FamilyTrip.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IRepository<User>? _users;
    private IRepository<Family>? _families;
    private IRepository<FamilyMember>? _familyMembers;
    private IRepository<Destination>? _destinations;
    private IRepository<TripPlan>? _tripPlans;
    private IRepository<ItineraryItem>? _itineraryItems;
    private IRepository<Recommendation>? _recommendations;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public IRepository<User> Users => _users ??= new Repository<User>(_context);
    public IRepository<Family> Families => _families ??= new Repository<Family>(_context);
    public IRepository<FamilyMember> FamilyMembers => _familyMembers ??= new Repository<FamilyMember>(_context);
    public IRepository<Destination> Destinations => _destinations ??= new Repository<Destination>(_context);
    public IRepository<TripPlan> TripPlans => _tripPlans ??= new Repository<TripPlan>(_context);
    public IRepository<ItineraryItem> ItineraryItems => _itineraryItems ??= new Repository<ItineraryItem>(_context);
    public IRepository<Recommendation> Recommendations => _recommendations ??= new Repository<Recommendation>(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
