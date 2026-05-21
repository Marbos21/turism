using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FamilyTrip.Application.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.AI.Anthropic;

public sealed class AnthropicChatClient : IAiChatClient
{
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly AiOptions _options;
    private readonly ILogger<AnthropicChatClient> _logger;

    public AnthropicChatClient(HttpClient http, IOptions<AiOptions> options, ILogger<AnthropicChatClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var systemBlocks = new List<object>
        {
            request.CacheSystemPrompt
                ? new { type = "text", text = request.SystemPrompt, cache_control = new { type = "ephemeral" } }
                : new { type = "text", text = request.SystemPrompt }
        };

        var tools = request.Tools.Select(t => new
        {
            name = t.Name,
            description = t.Description,
            input_schema = t.InputSchema
        }).ToArray();

        object? toolChoice = request.ForcedToolName is not null
            ? new { type = "tool", name = request.ForcedToolName }
            : (request.Tools.Count > 0 ? new { type = "auto" } : null);

        var body = new
        {
            model = request.Model,
            max_tokens = request.MaxOutputTokens,
            temperature = _options.Temperature,
            system = systemBlocks,
            messages = request.Messages.Select(MapMessage).ToArray(),
            tools = tools.Length > 0 ? tools : null,
            tool_choice = toolChoice
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anthropic call failed {Status}: {Body}", (int)response.StatusCode, raw);
            throw new HttpRequestException($"Anthropic API {(int)response.StatusCode}: {raw}");
        }

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        var content = new List<AiContentBlock>();
        if (root.TryGetProperty("content", out var contentArr))
        {
            foreach (var block in contentArr.EnumerateArray())
            {
                var type = block.GetProperty("type").GetString();
                if (type == "text")
                {
                    content.Add(new AiTextBlock(block.GetProperty("text").GetString() ?? string.Empty));
                }
                else if (type == "tool_use")
                {
                    content.Add(new AiToolUseBlock(
                        block.GetProperty("id").GetString() ?? string.Empty,
                        block.GetProperty("name").GetString() ?? string.Empty,
                        block.GetProperty("input").Clone()));
                }
            }
        }

        var usage = root.TryGetProperty("usage", out var usageEl)
            ? new AiUsage(
                InputTokens: usageEl.TryGetProperty("input_tokens", out var i) ? i.GetInt32() : 0,
                CachedInputTokens: usageEl.TryGetProperty("cache_read_input_tokens", out var c) ? c.GetInt32() : 0,
                OutputTokens: usageEl.TryGetProperty("output_tokens", out var o) ? o.GetInt32() : 0)
            : new AiUsage(0, 0, 0);

        var stopReason = root.TryGetProperty("stop_reason", out var sr) ? sr.GetString() : null;

        _logger.LogInformation(
            "Anthropic {Model} usage: input={Input} cached={Cached} output={Output} stop={Stop}",
            request.Model, usage.InputTokens, usage.CachedInputTokens, usage.OutputTokens, stopReason);

        return new AiChatResponse(stopReason, content, usage);
    }

    private static object MapMessage(AiMessage m) => new
    {
        role = m.Role,
        content = m.Content.Select<AiContentBlock, object>(b => b switch
        {
            AiTextBlock t => new { type = "text", text = t.Text },
            AiToolUseBlock u => new { type = "tool_use", id = u.Id, name = u.Name, input = u.Input },
            AiToolResultBlock r => new { type = "tool_result", tool_use_id = r.ToolUseId, content = r.Content, is_error = r.IsError },
            _ => throw new InvalidOperationException($"Unsupported content block: {b.GetType().Name}")
        }).ToArray()
    };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
