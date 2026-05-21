using System.Text.Json;

namespace FamilyTrip.Application.DTOs.Recommendation;

public class GenerateRecommendationsResponse
{
    public List<RecommendationDto> Recommendations { get; set; } = new();
    public AgentMetaDto? AgentMeta { get; set; }
}

public class AgentMetaDto
{
    public bool IsAiPowered { get; set; }
    public string? Model { get; set; }
    public int TurnsUsed { get; set; }
    public int TotalInputTokens { get; set; }
    public int TotalCachedTokens { get; set; }
    public int TotalOutputTokens { get; set; }
    public List<ToolCallTraceDto> ToolCalls { get; set; } = new();
}

public class ToolCallTraceDto
{
    public string Tool { get; set; } = string.Empty;
    public JsonElement Input { get; set; }
    public string Output { get; set; } = string.Empty;
    public int DurationMs { get; set; }
    public bool IsError { get; set; }
}
