using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyTrip.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<TripPlan> TripPlans => Set<TripPlan>();
    public DbSet<ItineraryItem> ItineraryItems => Set<ItineraryItem>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        SeedDestinations(modelBuilder);
    }

    private static void SeedDestinations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Destination>().HasData(
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000001"),
                Name = "Paris",
                Country = "France",
                Latitude = 48.8566,
                Longitude = 2.3522,
                Description = "The City of Light offers world-class museums, iconic landmarks, and exquisite cuisine. Family-friendly attractions include the Eiffel Tower, Louvre, and Disneyland Paris nearby.",
                Category = "Cultural",
                ChildFriendlyScore = 8,
                AvgCostPerDay = 250,
                Climate = Climate.Continental,
                ImageUrl = "https://images.unsplash.com/photo-1502602898657-3e91760cbb34",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000002"),
                Name = "Rome",
                Country = "Italy",
                Latitude = 41.9028,
                Longitude = 12.4964,
                Description = "The Eternal City captivates with ancient history, art, and incredible food. Kids love the Colosseum, Vatican Museums, and tossing coins in the Trevi Fountain.",
                Category = "Cultural",
                ChildFriendlyScore = 8,
                AvgCostPerDay = 200,
                Climate = Climate.Mediterranean,
                ImageUrl = "https://images.unsplash.com/photo-1515542622106-78bda8ba0e5b",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000003"),
                Name = "Barcelona",
                Country = "Spain",
                Latitude = 41.3851,
                Longitude = 2.1734,
                Description = "A vibrant city blending Gaudí's architectural wonders, beautiful beaches, and lively markets. Perfect for families seeking culture and sun.",
                Category = "Mixed",
                ChildFriendlyScore = 9,
                AvgCostPerDay = 180,
                Climate = Climate.Mediterranean,
                ImageUrl = "https://images.unsplash.com/photo-1583422409516-2895a77efded",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000004"),
                Name = "Santorini",
                Country = "Greece",
                Latitude = 36.3932,
                Longitude = 25.4615,
                Description = "Stunning volcanic island with iconic white-washed buildings, breathtaking sunsets, and crystal-clear Aegean waters. A romantic yet family-friendly destination.",
                Category = "Relaxation",
                ChildFriendlyScore = 7,
                AvgCostPerDay = 300,
                Climate = Climate.Mediterranean,
                ImageUrl = "https://images.unsplash.com/photo-1570077188670-e3a8d69ac5ff",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000005"),
                Name = "Bali",
                Country = "Indonesia",
                Latitude = -8.3405,
                Longitude = 115.0920,
                Description = "A tropical paradise with lush rice terraces, ancient temples, and warm hospitality. Families enjoy water parks, cultural experiences, and beautiful beaches.",
                Category = "Adventure",
                ChildFriendlyScore = 8,
                AvgCostPerDay = 120,
                Climate = Climate.Tropical,
                ImageUrl = "https://images.unsplash.com/photo-1537996194471-e657df975ab4",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000006"),
                Name = "Maldives",
                Country = "Maldives",
                Latitude = 3.2028,
                Longitude = 73.2207,
                Description = "A luxury island paradise with overwater bungalows, pristine coral reefs, and turquoise lagoons. Perfect for families seeking ultimate relaxation and water activities.",
                Category = "Relaxation",
                ChildFriendlyScore = 7,
                AvgCostPerDay = 600,
                Climate = Climate.Tropical,
                ImageUrl = "https://images.unsplash.com/photo-1514282401047-d79a71a590e8",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000007"),
                Name = "Tokyo",
                Country = "Japan",
                Latitude = 35.6762,
                Longitude = 139.6503,
                Description = "A futuristic metropolis that blends ancient traditions with cutting-edge technology. Kids are amazed by Disneyland Tokyo, Robot Restaurant, and unique pop culture.",
                Category = "Cultural",
                ChildFriendlyScore = 9,
                AvgCostPerDay = 280,
                Climate = Climate.Continental,
                ImageUrl = "https://images.unsplash.com/photo-1540959733332-eab4deabeeaf",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000008"),
                Name = "Costa Rica",
                Country = "Costa Rica",
                Latitude = 9.7489,
                Longitude = -83.7534,
                Description = "A biodiversity hotspot with rainforests, volcanoes, and gorgeous Pacific and Caribbean beaches. Families love zip-lining, wildlife tours, and surfing lessons.",
                Category = "Adventure",
                ChildFriendlyScore = 9,
                AvgCostPerDay = 150,
                Climate = Climate.Tropical,
                ImageUrl = "https://images.unsplash.com/photo-1518259102261-b40117eabbc9",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000009"),
                Name = "Amsterdam",
                Country = "Netherlands",
                Latitude = 52.3676,
                Longitude = 4.9041,
                Description = "A charming city of canals, museums, and bicycle culture. Families enjoy the Anne Frank House, Rijksmuseum, Vondelpark, and unique canal boat tours.",
                Category = "Cultural",
                ChildFriendlyScore = 8,
                AvgCostPerDay = 220,
                Climate = Climate.Continental,
                ImageUrl = "https://images.unsplash.com/photo-1512470876302-972faa2aa9a4",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Destination
            {
                Id = new Guid("00000001-0000-0000-0000-000000000010"),
                Name = "Iceland",
                Country = "Iceland",
                Latitude = 64.9631,
                Longitude = -19.0208,
                Description = "Land of fire and ice with dramatic landscapes, geysers, waterfalls, and the Northern Lights. An unforgettable adventure destination for nature-loving families.",
                Category = "Adventure",
                ChildFriendlyScore = 8,
                AvgCostPerDay = 350,
                Climate = Climate.Arctic,
                ImageUrl = "https://images.unsplash.com/photo-1529963183134-61a90db47eaf",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
