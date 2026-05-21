using FamilyTrip.Application.AI;
using FamilyTrip.Application.Integrations;
using FamilyTrip.Application.Services;
using FamilyTrip.Infrastructure.AI.Agents;
using FamilyTrip.Infrastructure.AI.Agents.Tools;
using FamilyTrip.Infrastructure.AI.Anthropic;
using FamilyTrip.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FamilyTrip.Infrastructure.AI;

public static class AiServiceCollectionExtensions
{
    public static IServiceCollection AddAiRecommendations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiOptions>()
            .Bind(configuration.GetSection(AiOptions.SectionName))
            .PostConfigure(opts =>
            {
                var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (!string.IsNullOrWhiteSpace(envKey))
                    opts.ApiKey = envKey;
            });

        services.AddHttpClient<IAiChatClient, AnthropicChatClient>((sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<AiOptions>>().Value;
            http.BaseAddress = new Uri("https://api.anthropic.com/");
            http.Timeout = TimeSpan.FromSeconds(opts.RequestTimeoutSeconds);
        });

        // Agent tools - registered once each. Order doesn't matter; the agent
        // looks them up by Name. Add a tool here = the agent can call it.
        services.AddScoped<IAgentTool, GetWeatherTool>();
        services.AddScoped<IAgentTool, GetPointsOfInterestTool>();
        services.AddScoped<IAgentTool, GetHotelAvailabilityTool>();
        services.AddScoped<IAgentTool, SubmitRecommendationsTool>();

        services.AddScoped<IRecommendationAgent, RecommendationAgent>();
        services.AddScoped<IDestinationDiscovery, DestinationDiscoveryAgent>();

        // One trace collector per request - the agent and the app service both grab the
        // same instance via DI.
        services.AddScoped<IAgentTraceCollector, AgentTraceCollector>();

        // Concrete rule-based engine is needed by the hybrid as a fallback.
        services.AddScoped<RecommendationEngine>();
        services.AddScoped<IRecommendationEngine, HybridRecommendationEngine>();

        return services;
    }
}
