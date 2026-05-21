using System.Text.Json;

namespace FamilyTrip.Application.AI;

public sealed record ToolCallTrace(
    string Tool,
    JsonElement Input,
    string Output,
    int DurationMs,
    bool IsError);

public sealed record AgentTrace(
    bool IsAiPowered,
    string? Model,
    int TurnsUsed,
    int TotalInputTokens,
    int TotalCachedTokens,
    int TotalOutputTokens,
    IReadOnlyList<ToolCallTrace> ToolCalls);

public interface IAgentTraceCollector
{
    void MarkAiPowered(string model);
    void RecordToolCall(string tool, JsonElement input, string output, int durationMs, bool isError);
    void RecordTurn(AiUsage usage);
    AgentTrace BuildTrace();
}
