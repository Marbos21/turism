using System.Text.Json;
using FamilyTrip.Application.AI;
using FamilyTrip.Application.Integrations;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Infrastructure.AI.Agents.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.AI;

public sealed class DestinationDiscoveryAgent : IDestinationDiscovery
{
    private static readonly JsonElement SubmitToolSchema = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "destinations": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "name":                { "type": "string" },
                  "country":             { "type": "string" },
                  "latitude":            { "type": "number", "minimum": -90,  "maximum": 90 },
                  "longitude":           { "type": "number", "minimum": -180, "maximum": 180 },
                  "description":         { "type": "string" },
                  "category":            { "type": "string", "enum": ["Adventure","Cultural","Relaxation","Mixed"] },
                  "childFriendlyScore":  { "type": "integer", "minimum": 1, "maximum": 10 },
                  "avgCostPerDay":       { "type": "number", "minimum": 30, "maximum": 1500 },
                  "climate":             { "type": "integer", "minimum": 1, "maximum": 4 },
                  "wikipediaTitle":      { "type": "string" }
                },
                "required": ["name","country","latitude","longitude","description","category","childFriendlyScore","avgCostPerDay","climate","wikipediaTitle"]
              }
            }
          },
          "required": ["destinations"]
        }
        """).RootElement.Clone();

    private readonly IAiChatClient _chat;
    private readonly AiOptions _options;
    private readonly ILogger<DestinationDiscoveryAgent> _logger;

    public DestinationDiscoveryAgent(
        IAiChatClient chat,
        IOptions<AiOptions> options,
        ILogger<DestinationDiscoveryAgent> logger)
    {
        _chat = chat;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.Enabled && !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<DiscoveredDestination>> DiscoverAsync(
        Family family,
        IReadOnlyList<FamilyMember> members,
        IReadOnlyList<Destination> existingDestinations,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || count <= 0) return Array.Empty<DiscoveredDestination>();

        var familyPayload = new
        {
            budgetLevel = family.BudgetLevel.ToString(),
            preferredClimate = family.PreferredClimate.ToString(),
            travelStyle = family.TravelStyle.ToString(),
            members = members.Select(m => new
            {
                age = GetAge(m.BirthDate),
                interests = m.Interests,
                specialNeeds = m.SpecialNeeds
            }).ToArray()
        };

        var existingPayload = existingDestinations
            .Select(d => $"{d.Name} ({d.Country})")
            .ToArray();

        var userText = $$"""
            Family profile:
            {{JsonSerializer.Serialize(familyPayload)}}

            Existing catalog (do NOT repeat any of these):
            {{JsonSerializer.Serialize(existingPayload)}}

            Propose exactly {{count}} new destinations following the schema.
            Call `submit_destinations` once.
            """;

        var request = new AiChatRequest(
            Model: _options.Model,
            SystemPrompt: DestinationDiscoveryPrompt.System,
            CacheSystemPrompt: true,
            Messages: new[] { new AiMessage("user", new AiContentBlock[] { new AiTextBlock(userText) }) },
            Tools: new[]
            {
                new AiTool(
                    Name: "submit_destinations",
                    Description: "Submit the proposed list of family-friendly destinations.",
                    InputSchema: SubmitToolSchema)
            },
            ForcedToolName: "submit_destinations",
            MaxOutputTokens: _options.MaxOutputTokens);

        try
        {
            var response = await _chat.CompleteAsync(request, cancellationToken);
            var toolUse = response.Content.OfType<AiToolUseBlock>().FirstOrDefault();
            if (toolUse is null) return Array.Empty<DiscoveredDestination>();

            return ParseDestinations(toolUse.Input, existingDestinations);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Destination discovery failed; returning empty set.");
            return Array.Empty<DiscoveredDestination>();
        }
    }

    private static IReadOnlyList<DiscoveredDestination> ParseDestinations(
        JsonElement input, IReadOnlyList<Destination> existing)
    {
        if (!input.TryGetProperty("destinations", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<DiscoveredDestination>();

        var existingKeys = existing
            .Select(d => Key(d.Name, d.Country))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new List<DiscoveredDestination>();
        var seenInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in arr.EnumerateArray())
        {
            try
            {
                var name = item.GetProperty("name").GetString() ?? "";
                var country = item.GetProperty("country").GetString() ?? "";
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(country)) continue;

                var key = Key(name, country);
                if (existingKeys.Contains(key) || !seenInBatch.Add(key)) continue;

                var lat = item.GetProperty("latitude").GetDouble();
                var lng = item.GetProperty("longitude").GetDouble();
                if (lat < -90 || lat > 90 || lng < -180 || lng > 180) continue;

                var category = item.GetProperty("category").GetString() ?? "Mixed";
                var score = Math.Clamp(item.GetProperty("childFriendlyScore").GetInt32(), 1, 10);
                var cost = (decimal)Math.Clamp(item.GetProperty("avgCostPerDay").GetDouble(), 30, 1500);
                var climate = Math.Clamp(item.GetProperty("climate").GetInt32(), 1, 4);
                var description = item.GetProperty("description").GetString() ?? "";
                var wiki = item.TryGetProperty("wikipediaTitle", out var w) ? w.GetString() : null;

                result.Add(new DiscoveredDestination(name, country, lat, lng, description, category, score, cost, climate, wiki));
            }
            catch
            {
                // Skip malformed entries silently - the agent occasionally drops a field.
            }
        }

        return result;
    }

    private static string Key(string name, string country) => $"{name.Trim().ToLowerInvariant()}|{country.Trim().ToLowerInvariant()}";

    private static int GetAge(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
