using System.Text.Json;
using FamilyTrip.Application.AI;

namespace FamilyTrip.Infrastructure.AI;

public sealed class AgentTraceCollector : IAgentTraceCollector
{
    private readonly List<ToolCallTrace> _toolCalls = new();
    private bool _isAiPowered;
    private string? _model;
    private int _turnsUsed;
    private int _inputTokens;
    private int _cachedTokens;
    private int _outputTokens;

    public void MarkAiPowered(string model)
    {
        _isAiPowered = true;
        _model = model;
    }

    public void RecordToolCall(string tool, JsonElement input, string output, int durationMs, bool isError)
    {
        _toolCalls.Add(new ToolCallTrace(tool, input.Clone(), output, durationMs, isError));
    }

    public void RecordTurn(AiUsage usage)
    {
        _turnsUsed++;
        _inputTokens += usage.InputTokens;
        _cachedTokens += usage.CachedInputTokens;
        _outputTokens += usage.OutputTokens;
    }

    public AgentTrace BuildTrace() => new(
        IsAiPowered: _isAiPowered,
        Model: _model,
        TurnsUsed: _turnsUsed,
        TotalInputTokens: _inputTokens,
        TotalCachedTokens: _cachedTokens,
        TotalOutputTokens: _outputTokens,
        ToolCalls: _toolCalls);
}
