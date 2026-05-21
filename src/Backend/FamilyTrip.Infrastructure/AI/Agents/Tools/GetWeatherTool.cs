using System.Text.Json;
using FamilyTrip.Application.Integrations;

namespace FamilyTrip.Infrastructure.AI.Agents.Tools;

public sealed class GetWeatherTool : IAgentTool
{
    private readonly IWeatherService _weather;

    public GetWeatherTool(IWeatherService weather) => _weather = weather;

    public string Name => "get_destination_weather";
    public string Description =>
        "Returns a 5-day weather summary for a candidate destination. Use this when the family's preferred climate is borderline-match and you want to confirm actual conditions.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
          "type": "object",
          "properties": {
            "destinationId": { "type": "string", "description": "Destination GUID from the candidates list." }
          },
          "required": ["destinationId"]
        }
        """).RootElement.Clone();

    public async Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentContext context, CancellationToken cancellationToken)
    {
        if (!_weather.IsConfigured)
            return ToolJson.Ok(new { available = false, reason = "weather service not configured" });

        if (!input.TryGetProperty("destinationId", out var idEl) || !Guid.TryParse(idEl.GetString(), out var id))
            return ToolJson.Error("invalid destinationId");

        if (!context.CandidatesById.TryGetValue(id, out var dest))
            return ToolJson.Error("destinationId is not in the candidate list");

        var snapshot = await _weather.GetForecastAsync(dest.Latitude, dest.Longitude, cancellationToken);
        if (snapshot is null) return ToolJson.Ok(new { available = false, reason = "no data" });

        return ToolJson.Ok(new
        {
            destinationId = id.ToString(),
            destination = dest.Name,
            avgTempC = snapshot.AverageTempCelsius,
            minTempC = snapshot.MinTempCelsius,
            maxTempC = snapshot.MaxTempCelsius,
            rainyDays = snapshot.RainyDays,
            summary = snapshot.Summary
        });
    }
}
