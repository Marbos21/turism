using System.Text.Json;
using FamilyTrip.Application.Integrations;

namespace FamilyTrip.Infrastructure.AI.Agents.Tools;

public sealed class GetPointsOfInterestTool : IAgentTool
{
    private readonly IMapsService _maps;

    public GetPointsOfInterestTool(IMapsService maps) => _maps = maps;

    public string Name => "get_points_of_interest";
    public string Description =>
        "Returns up to 8 nearby places at a destination for a given category. Use this to ground reasoning in concrete attractions the family will recognise. Categories: family_attractions, restaurants, parks, museums, beaches, theme_parks.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "destinationId": { "type": "string" },
            "kind": { "type": "string", "enum": ["family_attractions","restaurants","parks","museums","beaches","theme_parks"] }
          },
          "required": ["destinationId","kind"]
        }
        """).RootElement.Clone();

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentContext context, CancellationToken cancellationToken)
    {
        if (!_maps.IsConfigured)
            return ToolJson.Ok(new { available = false, reason = "maps service not configured" });

        if (!input.TryGetProperty("destinationId", out var idEl) || !Guid.TryParse(idEl.GetString(), out var id))
            return ToolJson.Error("invalid destinationId");
        if (!context.CandidatesById.TryGetValue(id, out var dest))
            return ToolJson.Error("destinationId is not in the candidate list");

        var kind = input.TryGetProperty("kind", out var k) ? k.GetString() ?? "family_attractions" : "family_attractions";
        var places = await _maps.GetNearbyPointsAsync(dest.Latitude, dest.Longitude, kind, limit: 8, cancellationToken);

        return ToolJson.Ok(new
        {
            destinationId = id.ToString(),
            destination = dest.Name,
            kind,
            places = places.Select(p => new { p.Name, p.Category, p.Rating, p.UserRatingsTotal }).ToArray()
        });
    }
}
