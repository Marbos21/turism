namespace FamilyTrip.Application.AI;

public class AiOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; set; } = false;
    public string Provider { get; set; } = "Anthropic";
    public string Model { get; set; } = "claude-haiku-4-5";
    public string? ApiKey { get; set; }
    public int MaxOutputTokens { get; set; } = 1500;
    public int MaxCandidates { get; set; } = 8;
    public double Temperature { get; set; } = 1.0;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int RecentRecommendationsTtlMinutes { get; set; } = 60;
}
