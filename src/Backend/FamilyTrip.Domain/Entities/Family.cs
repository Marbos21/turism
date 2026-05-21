using FamilyTrip.Domain.Enums;

namespace FamilyTrip.Domain.Entities;

public class Family : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public BudgetLevel BudgetLevel { get; set; }
    public Climate PreferredClimate { get; set; }
    public TravelStyle TravelStyle { get; set; }

    public User User { get; set; } = null!;
    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
    public ICollection<TripPlan> TripPlans { get; set; } = new List<TripPlan>();
    public ICollection<Recommendation> Recommendations { get; set; } = new List<Recommendation>();
}
