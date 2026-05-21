using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Infrastructure.Services;

public class RecommendationEngine : IRecommendationEngine
{
    public Task<List<ScoredDestination>> ScoreAsync(
        Family family,
        List<FamilyMember> members,
        List<Destination> destinations,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ScoredDestination>();
        var hasYoungChildren = members.Any(m => GetAge(m.BirthDate) < 6);
        var hasTeens = members.Any(m => GetAge(m.BirthDate) is >= 12 and <= 17);

        foreach (var destination in destinations)
        {
            double score = 0;
            var reasons = new List<string>();

            // Climate matching (0-25 points)
            if (destination.Climate == family.PreferredClimate)
            {
                score += 25;
                reasons.Add($"matches your preferred {family.PreferredClimate} climate");
            }
            else
            {
                score += 10;
            }

            // Travel style matching (0-20 points)
            var styleMatch = (family.TravelStyle, destination.Category) switch
            {
                (TravelStyle.Adventure, "Adventure") => 20,
                (TravelStyle.Relaxation, "Relaxation") => 20,
                (TravelStyle.Cultural, "Cultural") => 20,
                (TravelStyle.Mixed, _) => 15,
                (TravelStyle.Adventure, "Mixed") => 12,
                (TravelStyle.Cultural, "Mixed") => 12,
                _ => 5
            };
            score += styleMatch;
            if (styleMatch >= 15)
                reasons.Add($"excellent match for your {family.TravelStyle} travel style");

            // Budget matching (0-25 points)
            var budgetScore = family.BudgetLevel switch
            {
                BudgetLevel.Budget when destination.AvgCostPerDay <= 150 => 25,
                BudgetLevel.Budget when destination.AvgCostPerDay <= 250 => 15,
                BudgetLevel.Moderate when destination.AvgCostPerDay is > 100 and <= 300 => 25,
                BudgetLevel.Premium when destination.AvgCostPerDay is > 200 and <= 450 => 25,
                BudgetLevel.Luxury when destination.AvgCostPerDay > 400 => 25,
                BudgetLevel.Luxury when destination.AvgCostPerDay > 250 => 18,
                _ => 10
            };
            score += budgetScore;
            if (budgetScore >= 20)
                reasons.Add($"fits your {family.BudgetLevel} budget at ~${destination.AvgCostPerDay}/day");

            // Child-friendliness (0-20 points)
            var childScore = destination.ChildFriendlyScore switch
            {
                >= 9 => 20,
                >= 7 => 15,
                >= 5 => 8,
                _ => 3
            };
            if (hasYoungChildren && destination.ChildFriendlyScore >= 8)
            {
                childScore += 5;
                reasons.Add("highly rated for young children");
            }
            if (hasTeens && destination.Category == "Adventure")
            {
                childScore += 3;
                reasons.Add("great adventure activities for teens");
            }
            score += childScore;

            // Member interests bonus (0-10 points)
            var allInterests = members
                .Where(m => m.Interests != null)
                .SelectMany(m => m.Interests!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(i => i.Trim().ToLowerInvariant())
                .ToList();

            if (allInterests.Any(i => i.Contains("beach") || i.Contains("swimming")) &&
                (destination.Climate == Climate.Tropical || destination.Climate == Climate.Mediterranean))
            {
                score += 8;
                reasons.Add("beach and swimming opportunities");
            }
            if (allInterests.Any(i => i.Contains("history") || i.Contains("culture")) &&
                destination.Category == "Cultural")
            {
                score += 8;
                reasons.Add("rich cultural and historical attractions");
            }
            if (allInterests.Any(i => i.Contains("nature") || i.Contains("outdoor")) &&
                destination.Category == "Adventure")
            {
                score += 8;
                reasons.Add("outstanding nature and outdoor activities");
            }

            // Normalize to 0-100
            score = Math.Min(100, score);
            score = Math.Round(score, 1);

            var reasoning = reasons.Count > 0
                ? $"This destination scores {score}/100 because it {string.Join(", and ", reasons)}."
                : $"This destination scores {score}/100 overall for your family profile.";

            results.Add(new ScoredDestination(destination, score, reasoning));
        }

        return Task.FromResult(results.OrderByDescending(r => r.Score).ToList());
    }

    private static int GetAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
