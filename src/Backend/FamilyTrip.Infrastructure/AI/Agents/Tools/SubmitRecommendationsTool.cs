using System.Text.Json;
using FamilyTrip.Application.Services;

namespace FamilyTrip.Infrastructure.AI.Agents.Tools;

public sealed class SubmitRecommendationsTool : IAgentTool
{
    public string Name => "submit_recommendations";
    public string Description =>
        "TERMINAL TOOL. Call this exactly once when you have enough information. Submits the final ranked list of destinations for this family.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "rankings": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "destinationId": { "type": "string", "description": "Destination GUID, exactly as in the candidates list." },
                  "score": { "type": "number", "minimum": 0, "maximum": 100 },
                  "reasoning": { "type": "string", "description": "Why this destination fits this family. Cite concrete features (weather, POIs, hotels) if you queried them. 1-3 sentences, under 60 words." }
                },
                "required": ["destinationId","score","reasoning"]
              }
            }
          },
          "required": ["rankings"]
        }
        """).RootElement.Clone();

    public Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentContext context, CancellationToken cancellationToken)
    {
        if (!input.TryGetProperty("rankings", out var rankings) || rankings.ValueKind != JsonValueKind.Array)
            return Task.FromResult(ToolJson.Error("missing 'rankings' array"));

        var result = new List<ScoredDestination>();
        foreach (var item in rankings.EnumerateArray())
        {
            if (!item.TryGetProperty("destinationId", out var idEl)) continue;
            if (!Guid.TryParse(idEl.GetString(), out var id)) continue;
            if (!context.CandidatesById.TryGetValue(id, out var dest)) continue;

            var score = item.TryGetProperty("score", out var s) ? s.GetDouble() : 0;
            var reasoning = item.TryGetProperty("reasoning", out var r) ? r.GetString() ?? string.Empty : string.Empty;
            result.Add(new ScoredDestination(dest, Math.Clamp(Math.Round(score, 1), 0, 100), reasoning));
        }

        if (result.Count == 0)
            return Task.FromResult(ToolJson.Error("no valid rankings - destinationId values must match the candidates list"));

        return Task.FromResult(new AgentToolResult(
            Content: "{\"accepted\":true}",
            IsTerminal: true,
            Terminal: result.OrderByDescending(r => r.Score).ToList()));
    }
}
