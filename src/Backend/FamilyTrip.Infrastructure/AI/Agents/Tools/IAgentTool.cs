using System.Text.Json;
using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;

namespace FamilyTrip.Infrastructure.AI.Agents.Tools;

public sealed record AgentContext(
    Family Family,
    IReadOnlyList<FamilyMember> Members,
    IReadOnlyDictionary<Guid, Destination> CandidatesById);

public sealed record AgentToolResult(string Content, bool IsTerminal, IReadOnlyList<ScoredDestination>? Terminal = null, bool IsError = false);

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    Task<AgentToolResult> ExecuteAsync(JsonElement input, AgentContext context, CancellationToken cancellationToken);
}

internal static class ToolJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static AgentToolResult Error(string message) =>
        new(JsonSerializer.Serialize(new { error = message }), IsTerminal: false, IsError: true);

    public static AgentToolResult Ok(object payload) =>
        new(JsonSerializer.Serialize(payload, Options), IsTerminal: false);
}
