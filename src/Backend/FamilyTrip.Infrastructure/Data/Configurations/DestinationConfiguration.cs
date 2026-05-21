using FamilyTrip.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyTrip.Infrastructure.Data.Configurations;

public class DestinationConfiguration : IEntityTypeConfiguration<Destination>
{
    public void Configure(EntityTypeBuilder<Destination> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.AvgCostPerDay)
            .HasColumnType("decimal(18,2)");

        builder.Property(d => d.ImageUrl)
            .HasMaxLength(500);

        builder.HasMany(d => d.TripPlans)
            .WithOne(tp => tp.Destination)
            .HasForeignKey(tp => tp.DestinationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Recommendations)
            .WithOne(r => r.Destination)
            .HasForeignKey(r => r.DestinationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
