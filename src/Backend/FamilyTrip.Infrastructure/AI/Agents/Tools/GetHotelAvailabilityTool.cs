using System.Text.Json;
using FamilyTrip.Application.Integrations;

namespace FamilyTrip.Infrastructure.AI.Agents.Tools;

public sealed class GetHotelAvailabilityTool : IAgentTool
{
    private readonly IHotelService _hotels;

    public GetHotelAvailabilityTool(IHotelService hotels) => _hotels = hotels;

    public string Name => "get_hotel_availability";
    public string Description =>
        "Returns hotel supply (count + sample names with review scores) near a destination via the Duffel Stays API. Use sparingly - only when a family is Luxury tier or has specific accommodation needs that warrant verifying supply.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "destinationId": { "type": "string" }
          },
          "required": ["destinationId"]
        }
        """).RootElement.Clone();

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentContext context, CancellationToken cancellationToken)
    {
        if (!_hotels.IsConfigured)
            return ToolJson.Ok(new { available = false, reason = "hotel service not configured" });

        if (!input.TryGetProperty("destinationId", out var idEl) || !Guid.TryParse(idEl.GetString(), out var id))
            return ToolJson.Error("invalid destinationId");
        if (!context.CandidatesById.TryGetValue(id, out var dest))
            return ToolJson.Error("destinationId is not in the candidate list");

        var availability = await _hotels.GetAvailabilityAsync(dest.Latitude, dest.Longitude, radiusKm: 20, cancellationToken);
        if (availability is null) return ToolJson.Ok(new { available = false, reason = "no data" });

        return ToolJson.Ok(new
        {
            destinationId = id.ToString(),
            destination = dest.Name,
            hotelsFound = availability.HotelsFound,
            samples = availability.SampleHotels.Select(h => new { h.Name, h.Rating }).ToArray()
        });
    }
}
