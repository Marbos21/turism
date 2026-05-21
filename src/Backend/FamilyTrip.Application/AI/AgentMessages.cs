using System.Text.Json;

namespace FamilyTrip.Application.AI;

public abstract record AiContentBlock;
public sealed record AiTextBlock(string Text) : AiContentBlock;
public sealed record AiToolUseBlock(string Id, string Name, JsonElement Input) : AiContentBlock;
public sealed record AiToolResultBlock(string ToolUseId, string Content, bool IsError) : AiContentBlock;

public sealed record AiMessage(string Role, IReadOnlyList<AiContentBlock> Content);

public sealed record AiTool(string Name, string Description, JsonElement InputSchema);

public sealed record AiChatRequest(
    string Model,
    string SystemPrompt,
    bool CacheSystemPrompt,
    IReadOnlyList<AiMessage> Messages,
    IReadOnlyList<AiTool> Tools,
    string? ForcedToolName,
    int MaxOutputTokens);

public sealed record AiUsage(int InputTokens, int CachedInputTokens, int OutputTokens)
{
    public int TotalTokens => InputTokens + CachedInputTokens + OutputTokens;
}

public sealed record AiChatResponse(
    string? StopReason,
    IReadOnlyList<AiContentBlock> Content,
    AiUsage Usage);
