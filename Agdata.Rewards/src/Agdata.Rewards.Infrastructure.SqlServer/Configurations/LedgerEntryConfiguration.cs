using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agdata.Rewards.Infrastructure.SqlServer.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.UserId)
            .IsRequired();

        builder.Property(l => l.EventId);

        builder.Property(l => l.RedemptionRequestId);

        builder.Property(l => l.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(l => l.Points)
            .IsRequired();

        builder.Property(l => l.Timestamp)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LedgerEntries_Users");

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(l => l.EventId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LedgerEntries_Events");

        builder.HasOne<RedemptionRequest>()
            .WithMany()
            .HasForeignKey(l => l.RedemptionRequestId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_LedgerEntries_RedemptionRequests");

        builder.HasIndex(l => new { l.UserId, l.Timestamp, l.Id })
            .HasDatabaseName("IX_LedgerEntries_UserId_Timestamp_Id")
            .IsDescending(false, true, false);

        builder.HasIndex(l => l.EventId)
            .HasDatabaseName("IX_LedgerEntries_EventId");

        builder.HasIndex(l => l.RedemptionRequestId)
            .HasDatabaseName("IX_LedgerEntries_RedemptionRequestId");

        builder.ToTable(t => t.HasCheckConstraint("CK_LedgerEntries_Points", "[Points] > 0"));
    }
}

