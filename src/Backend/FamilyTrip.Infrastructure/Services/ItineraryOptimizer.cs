using FamilyTrip.Application.Services;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Infrastructure.Services;

public class ItineraryOptimizer : IItineraryOptimizer
{
    private static readonly Dictionary<string, List<(string Activity, string TimeSlot, int Duration, decimal CostMultiplier, string Notes)>> DestinationActivities = new()
    {
        ["Cultural"] = new()
        {
            ("Visit city museum", "Morning", 180, 0.05m, "Bring comfortable walking shoes"),
            ("Guided old town walking tour", "Morning", 120, 0.04m, "Book in advance"),
            ("Local market exploration", "Morning", 90, 0.02m, "Great for sampling local produce"),
            ("Cathedral / historic monument visit", "Morning", 60, 0.02m, "Check opening hours"),
            ("Traditional restaurant lunch", "Afternoon", 90, 0.10m, "Try local specialties"),
            ("Art gallery visit", "Afternoon", 120, 0.04m, "Family audio guides available"),
            ("Cultural performance / show", "Evening", 120, 0.12m, "Book tickets ahead"),
            ("Sunset viewpoint", "Evening", 60, 0.0m, "Free and spectacular"),
            ("Local street food tour", "Evening", 90, 0.06m, "Kids love the variety")
        },
        ["Adventure"] = new()
        {
            ("Hiking trail exploration", "Morning", 180, 0.03m, "Pack water and snacks"),
            ("Zip-lining experience", "Morning", 120, 0.15m, "Age and weight restrictions apply"),
            ("Wildlife spotting tour", "Morning", 150, 0.10m, "Bring binoculars"),
            ("Kayaking / water sports", "Afternoon", 120, 0.12m, "Life jackets provided"),
            ("Rock climbing session", "Afternoon", 90, 0.10m, "Beginner-friendly options available"),
            ("Sunset beach bonfire", "Evening", 90, 0.05m, "Magical experience for kids"),
            ("Night nature walk", "Evening", 60, 0.04m, "Bring flashlights"),
            ("Surfing lesson", "Morning", 120, 0.12m, "All levels welcome")
        },
        ["Relaxation"] = new()
        {
            ("Morning beach walk", "Morning", 60, 0.0m, "Peaceful and rejuvenating"),
            ("Spa & wellness session", "Morning", 120, 0.20m, "Family packages available"),
            ("Boat trip / island hopping", "Morning", 180, 0.15m, "Pack sunscreen"),
            ("Pool relaxation", "Afternoon", 120, 0.0m, "Included with accommodation"),
            ("Snorkeling experience", "Afternoon", 90, 0.08m, "Equipment often included"),
            ("Sunset cruise", "Evening", 90, 0.15m, "Romantic and memorable"),
            ("Fine dining experience", "Evening", 120, 0.20m, "Reserve ahead for best tables"),
            ("Beach volleyball / games", "Afternoon", 60, 0.0m, "Fun for the whole family")
        },
        ["Mixed"] = new()
        {
            ("Morning city exploration", "Morning", 120, 0.02m, "Download city walking map"),
            ("Family theme park", "Morning", 240, 0.25m, "Book fast-pass tickets"),
            ("Scenic viewpoint", "Morning", 60, 0.0m, "Perfect for photos"),
            ("Local cuisine cooking class", "Afternoon", 150, 0.12m, "Kids love learning to cook"),
            ("Beach / waterfront time", "Afternoon", 120, 0.0m, "Bring towels and sunscreen"),
            ("Evening food market", "Evening", 90, 0.06m, "Great street food scene"),
            ("Cultural show / performance", "Evening", 90, 0.10m, "Book in advance"),
            ("Bicycle tour", "Morning", 120, 0.05m, "Family bikes available")
        }
    };

    public Task<List<ItineraryItem>> GenerateItineraryAsync(
        TripPlan plan, Destination destination, Family family, List<FamilyMember> members,
        CancellationToken cancellationToken = default)
    {
        var items = new List<ItineraryItem>();
        var totalDays = (int)(plan.EndDate - plan.StartDate).TotalDays;
        if (totalDays <= 0) totalDays = 1;

        var category = destination.Category;
        if (!DestinationActivities.ContainsKey(category))
            category = "Mixed";

        var activities = DestinationActivities[category];
        var dailyBudget = totalDays > 0 ? plan.EstimatedBudget / totalDays : plan.EstimatedBudget;

        var hasYoungChildren = members.Any(m => GetAge(m.BirthDate) < 6);
        var timeSlots = new[] { "Morning", "Afternoon", "Evening" };

        for (int day = 1; day <= totalDays; day++)
        {
            var dayActivities = activities
                .GroupBy(a => a.TimeSlot)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var slot in timeSlots)
            {
                if (!dayActivities.ContainsKey(slot) || !dayActivities[slot].Any())
                    continue;

                // Pick an activity for this slot (rotate by day)
                var slotActivities = dayActivities[slot];
                var activityIndex = (day - 1) % slotActivities.Count;
                var activity = slotActivities[activityIndex];

                // Skip evening activities if there are very young children on some days
                if (slot == "Evening" && hasYoungChildren && day % 3 != 0)
                    continue;

                var estimatedCost = Math.Round(dailyBudget * activity.CostMultiplier, 2);

                items.Add(new ItineraryItem
                {
                    TripPlanId = plan.Id,
                    DayNumber = day,
                    TimeSlot = slot,
                    ActivityName = activity.Activity,
                    Location = destination.Name,
                    Duration = activity.Duration,
                    EstimatedCost = estimatedCost,
                    Notes = activity.Notes
                });
            }
        }

        return Task.FromResult(items);
    }

    private static int GetAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
