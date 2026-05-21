using FamilyTrip.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyTrip.Infrastructure.Data.Configurations;

public class TripPlanConfiguration : IEntityTypeConfiguration<TripPlan>
{
    public void Configure(EntityTypeBuilder<TripPlan> builder)
    {
        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tp => tp.EstimatedBudget)
            .HasColumnType("decimal(18,2)");

        builder.Property(tp => tp.Status)
            .IsRequired();

        builder.HasMany(tp => tp.ItineraryItems)
            .WithOne(i => i.TripPlan)
            .HasForeignKey(i => i.TripPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
