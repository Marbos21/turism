using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace FamilyTrip.Infrastructure.AI;

public sealed class HybridItineraryOptimizer : IItineraryOptimizer
{
    private readonly AiItineraryOptimizer _ai;
    private readonly ItineraryOptimizer _ruleBased;
    private readonly ILogger<HybridItineraryOptimizer> _logger;

    public HybridItineraryOptimizer(
        AiItineraryOptimizer ai,
        ItineraryOptimizer ruleBased,
        ILogger<HybridItineraryOptimizer> logger)
    {
        _ai = ai;
        _ruleBased = ruleBased;
        _logger = logger;
    }

    public async Task<List<ItineraryItem>> GenerateItineraryAsync(
        TripPlan plan, Destination destination, Family family, List<FamilyMember> members,
        CancellationToken cancellationToken = default)
    {
        if (_ai.IsConfigured)
        {
            var aiItems = await _ai.TryGenerateAsync(plan, destination, family, members, cancellationToken);
            if (aiItems is { Count: > 0 })
            {
                _logger.LogInformation("AI itinerary returned {Count} activities.", aiItems.Count);
                return aiItems;
            }
            _logger.LogInformation("AI itinerary unavailable or empty; falling back to rule-based.");
        }

        return await _ruleBased.GenerateItineraryAsync(plan, destination, family, members, cancellationToken);
    }
}
