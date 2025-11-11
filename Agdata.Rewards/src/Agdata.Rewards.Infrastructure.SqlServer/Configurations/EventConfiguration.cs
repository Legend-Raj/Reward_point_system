using Agdata.Rewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agdata.Rewards.Infrastructure.SqlServer.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.OccursAt)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(e => new { e.IsActive, e.OccursAt })
            .HasDatabaseName("IX_Events_IsActive_OccursAt")
            .IsDescending(false, true);

        builder.HasIndex(e => e.OccursAt)
            .HasDatabaseName("IX_Events_OccursAt")
            .IsDescending(true);
    }
}

