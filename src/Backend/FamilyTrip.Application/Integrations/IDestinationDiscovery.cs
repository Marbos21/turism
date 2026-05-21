using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Application.Integrations;

public sealed record DiscoveredDestination(
    string Name,
    string Country,
    double Latitude,
    double Longitude,
    string Description,
    string Category,
    int ChildFriendlyScore,
    decimal AvgCostPerDay,
    int Climate,
    string? WikipediaTitle);

public interface IDestinationDiscovery
{
    bool IsConfigured { get; }

    Task<IReadOnlyList<DiscoveredDestination>> DiscoverAsync(
        Family family,
        IReadOnlyList<FamilyMember> members,
        IReadOnlyList<Destination> existingDestinations,
        int count,
        CancellationToken cancellationToken = default);
}

public interface IWikipediaImageResolver
{
    Task<string?> GetThumbnailUrlAsync(string title, CancellationToken cancellationToken = default);
}
