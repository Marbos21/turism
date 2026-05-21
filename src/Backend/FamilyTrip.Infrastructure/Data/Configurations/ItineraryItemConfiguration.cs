using FamilyTrip.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyTrip.Infrastructure.Data.Configurations;

public class ItineraryItemConfiguration : IEntityTypeConfiguration<ItineraryItem>
{
    public void Configure(EntityTypeBuilder<ItineraryItem> builder)
    {
        builder.HasKey(i => i.Id);

        builder.Property(i => i.TimeSlot)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(i => i.ActivityName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.EstimatedCost)
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Notes)
            .HasMaxLength(500);
    }
}
