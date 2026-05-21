using FamilyTrip.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyTrip.Infrastructure.Data.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.BudgetLevel)
            .IsRequired();

        builder.Property(f => f.PreferredClimate)
            .IsRequired();

        builder.Property(f => f.TravelStyle)
            .IsRequired();

        builder.HasMany(f => f.Members)
            .WithOne(m => m.Family)
            .HasForeignKey(m => m.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.TripPlans)
            .WithOne(tp => tp.Family)
            .HasForeignKey(tp => tp.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(f => f.Recommendations)
            .WithOne(r => r.Family)
            .HasForeignKey(r => r.FamilyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
