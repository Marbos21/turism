using System.Text.Json;
using FamilyTrip.Application.AI;
using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Infrastructure.AI.Agents.Prompts;
using FamilyTrip.Infrastructure.AI.Agents.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.AI.Agents;

public interface IRecommendationAgent
{
    Task<List<ScoredDestination>> RankAsync(
        Family family,
        List<FamilyMember> members,
        List<Destination> candidates,
        CancellationToken cancellationToken = default);
}

public sealed class RecommendationAgent : IRecommendationAgent
{
    private const int MaxTurns = 5;
    private const int MaxToolCalls = 8;

    private readonly IAiChatClient _chat;
    private readonly IReadOnlyDictionary<string, IAgentTool> _toolsByName;
    private readonly IReadOnlyList<AiTool> _aiTools;
    private readonly AiOptions _options;
    private readonly IAgentTraceCollector _trace;
    private readonly ILogger<RecommendationAgent> _logger;

    public RecommendationAgent(
        IAiChatClient chat,
        IEnumerable<IAgentTool> tools,
        IOptions<AiOptions> options,
        IAgentTraceCollector trace,
        ILogger<RecommendationAgent> logger)
    {
        _chat = chat;
        _options = options.Value;
        _trace = trace;
        _logger = logger;

        var toolList = tools.ToList();
        _toolsByName = toolList.ToDictionary(t => t.Name);
        _aiTools = toolList.Select(t => new AiTool(t.Name, t.Description, t.InputSchema)).ToList();
    }

    public async Task<List<ScoredDestination>> RankAsync(
        Family family,
        List<FamilyMember> members,
        List<Destination> candidates,
        CancellationToken cancellationToken = default)
    {
        if (candidates.Count == 0) return new List<ScoredDestination>();

        var context = new AgentContext(
            Family: family,
            Members: members,
            CandidatesById: candidates.ToDictionary(c => c.Id));

        var initialUserText = BuildInitialUserMessage(family, members, candidates);
        var messages = new List<AiMessage>
        {
            new("user", new AiContentBlock[] { new AiTextBlock(initialUserText) })
        };

        var totalToolCalls = 0;
        var usage = new AiUsage(0, 0, 0);

        for (var turn = 0; turn < MaxTurns; turn++)
        {
            var request = new AiChatRequest(
                Model: _options.Model,
                SystemPrompt: RecommendationAgentPrompt.System,
                CacheSystemPrompt: true,
                Messages: messages,
                Tools: _aiTools,
                ForcedToolName: null,
                MaxOutputTokens: _options.MaxOutputTokens);

            var response = await _chat.CompleteAsync(request, cancellationToken);
            usage = new AiUsage(
                usage.InputTokens + response.Usage.InputTokens,
                usage.CachedInputTokens + response.Usage.CachedInputTokens,
                usage.OutputTokens + response.Usage.OutputTokens);
            _trace.RecordTurn(response.Usage);
            if (turn == 0) _trace.MarkAiPowered(_options.Model);

            var toolUses = response.Content.OfType<AiToolUseBlock>().ToList();
            if (toolUses.Count == 0)
            {
                _logger.LogWarning("Agent ended without calling submit_recommendations. stop={Stop}", response.StopReason);
                throw new InvalidOperationException("Agent did not call submit_recommendations.");
            }

            // Add assistant's response (which includes any text + the tool_use blocks) to history.
            messages.Add(new AiMessage("assistant", response.Content));

            var toolResults = new List<AiContentBlock>();
            foreach (var toolUse in toolUses)
            {
                if (++totalToolCalls > MaxToolCalls)
                {
                    _logger.LogWarning("Tool-call budget exceeded ({Calls}). Aborting agent.", totalToolCalls);
                    throw new InvalidOperationException("Agent exceeded tool-call budget.");
                }

                if (!_toolsByName.TryGetValue(toolUse.Name, out var tool))
                {
                    toolResults.Add(new AiToolResultBlock(toolUse.Id, $"{{\"error\":\"unknown tool {toolUse.Name}\"}}", IsError: true));
                    continue;
                }

                AgentToolResult result;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    result = await tool.ExecuteAsync(toolUse.Input, context, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Tool {Tool} threw; reporting error to agent.", toolUse.Name);
                    result = ToolJson.Error("tool execution failed");
                }
                sw.Stop();
                _trace.RecordToolCall(toolUse.Name, toolUse.Input, result.Content, (int)sw.ElapsedMilliseconds, result.IsError);

                if (result.IsTerminal && result.Terminal is not null)
                {
                    _logger.LogInformation(
                        "Agent finished after {Turns} turn(s), {Tools} tool call(s). Tokens: in={In} cached={Cached} out={Out}",
                        turn + 1, totalToolCalls, usage.InputTokens, usage.CachedInputTokens, usage.OutputTokens);
                    return result.Terminal.ToList();
                }

                toolResults.Add(new AiToolResultBlock(toolUse.Id, result.Content, result.IsError));
            }

            messages.Add(new AiMessage("user", toolResults));
        }

        throw new InvalidOperationException($"Agent did not finish within {MaxTurns} turns.");
    }

    private static string BuildInitialUserMessage(Family family, List<FamilyMember> members, List<Destination> candidates)
    {
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

        var candidatesPayload = candidates.Select(d => new
        {
            id = d.Id.ToString(),
            name = d.Name,
            country = d.Country,
            category = d.Category,
            climate = d.Climate.ToString(),
            avgCostPerDay = d.AvgCostPerDay,
            childFriendlyScore = d.ChildFriendlyScore,
            description = d.Description
        }).ToArray();

        return $$"""
            Family profile:
            {{JsonSerializer.Serialize(familyPayload, JsonOpts)}}

            Candidate destinations:
            {{JsonSerializer.Serialize(candidatesPayload, JsonOpts)}}

            Score and rank every candidate. Use tools only when they add real signal.
            Call `submit_recommendations` when ready.
            """;
    }

    private static int GetAge(DateTime birthDate)
    {
        var today = DateTime.UtcNow.Date;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };
}
