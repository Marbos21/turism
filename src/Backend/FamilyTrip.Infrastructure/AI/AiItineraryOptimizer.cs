using System.Text.Json;
using FamilyTrip.Application.AI;
using FamilyTrip.Application.Integrations;
using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Infrastructure.AI.Agents.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.AI;

public sealed class AiItineraryOptimizer
{
    private static readonly JsonElement SubmitToolSchema = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "items": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "dayNumber":              { "type": "integer", "minimum": 1 },
                  "timeSlot":               { "type": "string", "enum": ["Morning","Afternoon","Evening"] },
                  "activityName":           { "type": "string" },
                  "location":               { "type": "string" },
                  "durationMinutes":        { "type": "integer", "minimum": 15, "maximum": 480 },
                  "estimatedCostPerPerson": { "type": "number", "minimum": 0, "maximum": 500 },
                  "notes":                  { "type": "string" }
                },
                "required": ["dayNumber","timeSlot","activityName","durationMinutes","estimatedCostPerPerson"]
              }
            }
          },
          "required": ["items"]
        }
        """).RootElement.Clone();

    private readonly IAiChatClient _chat;
    private readonly IMapsService _maps;
    private readonly AiOptions _options;
    private readonly ILogger<AiItineraryOptimizer> _logger;

    public AiItineraryOptimizer(
        IAiChatClient chat,
        IMapsService maps,
        IOptions<AiOptions> options,
        ILogger<AiItineraryOptimizer> logger)
    {
        _chat = chat;
        _maps = maps;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<List<ItineraryItem>?> TryGenerateAsync(
        TripPlan plan, Destination destination, Family family, List<FamilyMember> members,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured) return null;

        var totalDays = Math.Max(1, (int)(plan.EndDate - plan.StartDate).TotalDays);
        totalDays = Math.Min(totalDays, 14); // hard cap so token budget stays sane

        var venues = await GatherVenuesAsync(destination, cancellationToken);

        var familyPayload = new
        {
            budgetLevel = family.BudgetLevel.ToString(),
            travelStyle = family.TravelStyle.ToString(),
            members = members.Select(m => new
            {
                age = GetAge(m.BirthDate),
                interests = m.Interests,
                specialNeeds = m.SpecialNeeds
            }).ToArray()
        };

        var userText = $$"""
            Trip:
            - Destination: {{destination.Name}}, {{destination.Country}}
            - Duration: {{totalDays}} day(s) ({{plan.StartDate:yyyy-MM-dd}} to {{plan.EndDate:yyyy-MM-dd}})
            - Total budget guidance: ~${{plan.EstimatedBudget}} for the whole trip

            Family:
            {{JsonSerializer.Serialize(familyPayload)}}

            Available venues (real Google Places data, use these as activities when they fit):
            {{JsonSerializer.Serialize(venues)}}

            Plan {{totalDays}} days. Call `submit_itinerary` once.
            """;

        var request = new AiChatRequest(
            Model: _options.Model,
            SystemPrompt: ItineraryAgentPrompt.System,
            CacheSystemPrompt: true,
            Messages: new[] { new AiMessage("user", new AiContentBlock[] { new AiTextBlock(userText) }) },
            Tools: new[]
            {
                new AiTool(
                    Name: "submit_itinerary",
                    Description: "Submit the full day-by-day itinerary.",
                    InputSchema: SubmitToolSchema)
            },
            ForcedToolName: "submit_itinerary",
            MaxOutputTokens: Math.Max(_options.MaxOutputTokens, 2500));

        try
        {
            var response = await _chat.CompleteAsync(request, cancellationToken);
            var toolUse = response.Content.OfType<AiToolUseBlock>().FirstOrDefault();
            if (toolUse is null) return null;

            return ParseItems(toolUse.Input, plan.Id, totalDays, destination.Name);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AI itinerary generation failed; falling back to rule-based.");
            return null;
        }
    }

    private async Task<object> GatherVenuesAsync(Destination dest, CancellationToken ct)
    {
        if (!_maps.IsConfigured) return Array.Empty<object>();

        var kinds = new[] { "family_attractions", "restaurants", "parks", "museums" };
        var tasks = kinds.Select(k => _maps.GetNearbyPointsAsync(dest.Latitude, dest.Longitude, k, 6, ct));
        var results = await Task.WhenAll(tasks);

        return kinds.Zip(results, (kind, pois) => new
        {
            kind,
            places = pois.Select(p => new { name = p.Name, category = p.Category, rating = p.Rating }).ToArray()
        }).Where(g => ((dynamic)g).places.Length > 0).ToArray();
    }

    private static List<ItineraryItem> ParseItems(JsonElement input, Guid tripPlanId, int maxDays, string defaultLocation)
    {
        if (!input.TryGetProperty("items", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return new List<ItineraryItem>();

        var result = new List<ItineraryItem>();
        foreach (var item in arr.EnumerateArray())
        {
            try
            {
                var day = item.GetProperty("dayNumber").GetInt32();
                if (day < 1 || day > maxDays) continue;

                var slot = item.GetProperty("timeSlot").GetString() ?? "Morning";
                if (slot is not ("Morning" or "Afternoon" or "Evening")) slot = "Morning";

                var name = item.GetProperty("activityName").GetString();
                if (string.IsNullOrWhiteSpace(name)) continue;

                var duration = Math.Clamp(item.GetProperty("durationMinutes").GetInt32(), 15, 480);
                var cost = (decimal)Math.Clamp(item.GetProperty("estimatedCostPerPerson").GetDouble(), 0, 500);
                var location = item.TryGetProperty("location", out var locEl) ? locEl.GetString() : null;
                var notes = item.TryGetProperty("notes", out var notesEl) ? notesEl.GetString() : null;

                result.Add(new ItineraryItem
                {
                    TripPlanId = tripPlanId,
                    DayNumber = day,
                    TimeSlot = slot,
                    ActivityName = name,
                    Location = string.IsNullOrWhiteSpace(location) ? defaultLocation : location,
                    Duration = duration,
                    EstimatedCost = Math.Round(cost, 2),
                    Notes = notes
                });
            }
            catch
            {
                // Skip malformed entries
            }
        }
        return result;
    }

    private static int GetAge(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
