using FamilyTrip.Application.AI;
using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Infrastructure.AI.Agents;
using FamilyTrip.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.AI;

public sealed class HybridRecommendationEngine : IRecommendationEngine
{
    private readonly RecommendationEngine _ruleBased;
    private readonly IRecommendationAgent _agent;
    private readonly AiOptions _options;
    private readonly ILogger<HybridRecommendationEngine> _logger;

    public HybridRecommendationEngine(
        RecommendationEngine ruleBased,
        IRecommendationAgent agent,
        IOptions<AiOptions> options,
        ILogger<HybridRecommendationEngine> logger)
    {
        _ruleBased = ruleBased;
        _agent = agent;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<ScoredDestination>> ScoreAsync(
        Family family,
        List<FamilyMember> members,
        List<Destination> destinations,
        CancellationToken cancellationToken = default)
    {
        var ruleBasedRanking = await _ruleBased.ScoreAsync(family, members, destinations, cancellationToken);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            _logger.LogInformation("AI disabled - serving rule-based recommendations.");
            return ruleBasedRanking;
        }

        var candidates = ruleBasedRanking
            .Take(_options.MaxCandidates)
            .Select(r => r.Destination)
            .ToList();

        if (candidates.Count == 0) return ruleBasedRanking;

        try
        {
            var agentRanking = await _agent.RankAsync(family, members, candidates, cancellationToken);
            if (agentRanking.Count == 0)
            {
                _logger.LogWarning("Agent returned empty ranking; falling back to rule-based.");
                return ruleBasedRanking;
            }
            return agentRanking;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Agent failed; falling back to rule-based recommendations.");
            return ruleBasedRanking;
        }
    }
}
