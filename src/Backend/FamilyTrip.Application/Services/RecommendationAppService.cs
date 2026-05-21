using AutoMapper;
using FamilyTrip.Application.AI;
using FamilyTrip.Application.DTOs.Destination;
using FamilyTrip.Application.DTOs.Recommendation;
using FamilyTrip.Application.Integrations;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Enums;
using FamilyTrip.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Application.Services;

public class RecommendationAppService : IRecommendationAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IAgentTraceCollector _traceCollector;
    private readonly IMemoryCache _cache;
    private readonly AiOptions _aiOptions;
    private readonly IDestinationDiscovery _discovery;
    private readonly IWikipediaImageResolver _imageResolver;

    public RecommendationAppService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IRecommendationEngine recommendationEngine,
        IAgentTraceCollector traceCollector,
        IMemoryCache cache,
        IOptions<AiOptions> aiOptions,
        IDestinationDiscovery discovery,
        IWikipediaImageResolver imageResolver)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _recommendationEngine = recommendationEngine;
        _traceCollector = traceCollector;
        _cache = cache;
        _aiOptions = aiOptions.Value;
        _discovery = discovery;
        _imageResolver = imageResolver;
    }

    private const int MinEligiblePoolSize = 3;
    private const int TargetEligiblePoolSize = 8;
    private static string RecentRecsCacheKey(Guid familyId) => $"recent_recs:{familyId}";

    public async Task<GenerateRecommendationsResponse> GenerateAsync(GenerateRecommendationsRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(request.FamilyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var members = await _unitOfWork.FamilyMembers.FindAsync(m => m.FamilyId == request.FamilyId, cancellationToken);
        var allDestinations = (await _unitOfWork.Destinations.GetAllAsync(cancellationToken)).ToList();

        // Remove old recommendations for this family
        var oldRecs = await _unitOfWork.Recommendations.FindAsync(r => r.FamilyId == request.FamilyId, cancellationToken);
        foreach (var old in oldRecs)
            await _unitOfWork.Recommendations.DeleteAsync(old, cancellationToken);

        // Exclude destinations that were recommended for this family in the last hour
        // so consecutive "Generate" clicks surface different options. When the eligible
        // pool gets too small, reset and start the rotation over.
        var recentKey = RecentRecsCacheKey(request.FamilyId);
        var recentlyRecommended = _cache.Get<HashSet<Guid>>(recentKey) ?? new HashSet<Guid>();
        var eligibleDestinations = allDestinations.Where(d => !recentlyRecommended.Contains(d.Id)).ToList();
        if (eligibleDestinations.Count < MinEligiblePoolSize)
        {
            recentlyRecommended.Clear();
            eligibleDestinations = allDestinations.ToList();
        }

        // If the pool is still thin, ask the discovery agent to invent fresh
        // destinations the family hasn't seen. Persist them immediately so they
        // become part of the permanent catalog and slot into the eligible pool.
        if (eligibleDestinations.Count < TargetEligiblePoolSize && _discovery.IsConfigured)
        {
            var missing = TargetEligiblePoolSize - eligibleDestinations.Count;
            var discovered = await _discovery.DiscoverAsync(
                family, members.ToList(), allDestinations, missing, cancellationToken);

            foreach (var dd in discovered)
            {
                var imageUrl = string.IsNullOrWhiteSpace(dd.WikipediaTitle)
                    ? null
                    : await _imageResolver.GetThumbnailUrlAsync(dd.WikipediaTitle!, cancellationToken);

                var entity = new Destination
                {
                    Id = Guid.NewGuid(),
                    Name = dd.Name,
                    Country = dd.Country,
                    Latitude = dd.Latitude,
                    Longitude = dd.Longitude,
                    Description = dd.Description,
                    Category = dd.Category,
                    ChildFriendlyScore = dd.ChildFriendlyScore,
                    AvgCostPerDay = dd.AvgCostPerDay,
                    Climate = (Climate)dd.Climate,
                    ImageUrl = imageUrl ?? string.Empty
                };
                await _unitOfWork.Destinations.AddAsync(entity, cancellationToken);
                eligibleDestinations.Add(entity);
            }

            if (discovered.Count > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Generate new recommendations against the eligible pool only
        var scored = await _recommendationEngine.ScoreAsync(family, members.ToList(), eligibleDestinations, cancellationToken);

        var recommendations = new List<Recommendation>();
        foreach (var scoredDestination in scored.Take(5))
        {
            var rec = new Recommendation
            {
                Id = Guid.NewGuid(),
                FamilyId = request.FamilyId,
                DestinationId = scoredDestination.Destination.Id,
                Score = scoredDestination.Score,
                Reasoning = scoredDestination.Reasoning,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Recommendations.AddAsync(rec, cancellationToken);
            recommendations.Add(rec);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Remember what we just returned so the next generate avoids them.
        foreach (var rec in recommendations)
            recentlyRecommended.Add(rec.DestinationId);
        _cache.Set(recentKey, recentlyRecommended, TimeSpan.FromMinutes(_aiOptions.RecentRecommendationsTtlMinutes));

        var result = new List<RecommendationDto>();
        foreach (var rec in recommendations)
        {
            var dto = _mapper.Map<RecommendationDto>(rec);
            var destination = await _unitOfWork.Destinations.GetByIdAsync(rec.DestinationId, cancellationToken);
            if (destination is not null)
                dto.Destination = _mapper.Map<DestinationDto>(destination);
            result.Add(dto);
        }

        var trace = _traceCollector.BuildTrace();
        return new GenerateRecommendationsResponse
        {
            Recommendations = result,
            AgentMeta = new AgentMetaDto
            {
                IsAiPowered = trace.IsAiPowered,
                Model = trace.Model,
                TurnsUsed = trace.TurnsUsed,
                TotalInputTokens = trace.TotalInputTokens,
                TotalCachedTokens = trace.TotalCachedTokens,
                TotalOutputTokens = trace.TotalOutputTokens,
                ToolCalls = trace.ToolCalls.Select(tc => new ToolCallTraceDto
                {
                    Tool = tc.Tool,
                    Input = tc.Input,
                    Output = tc.Output,
                    DurationMs = tc.DurationMs,
                    IsError = tc.IsError
                }).ToList()
            }
        };
    }

    public async Task<List<RecommendationDto>> GetByFamilyAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var recs = await _unitOfWork.Recommendations.FindAsync(r => r.FamilyId == familyId, cancellationToken);
        var result = new List<RecommendationDto>();

        foreach (var rec in recs.OrderByDescending(r => r.Score))
        {
            var dto = _mapper.Map<RecommendationDto>(rec);
            var destination = await _unitOfWork.Destinations.GetByIdAsync(rec.DestinationId, cancellationToken);
            if (destination is not null)
                dto.Destination = _mapper.Map<DestinationDto>(destination);
            result.Add(dto);
        }

        return result;
    }
}
