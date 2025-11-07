using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agdata.Rewards.Infrastructure.SqlServer.Configurations;

public class RedemptionRequestConfiguration : IEntityTypeConfiguration<RedemptionRequest>
{
    public void Configure(EntityTypeBuilder<RedemptionRequest> builder)
    {
        builder.ToTable("RedemptionRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.ProductId)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(RedemptionRequestStatus.Pending);

        builder.Property(r => r.RequestedAt)
            .IsRequired();

        builder.Property(r => r.ApprovedAt);

        builder.Property(r => r.DeliveredAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RedemptionRequests_Users");

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_RedemptionRequests_Products");

        builder.HasIndex(r => new { r.UserId, r.ProductId, r.Status })
            .HasDatabaseName("IX_RedemptionRequests_UserId_ProductId_Status");

        builder.HasIndex(r => new { r.ProductId, r.Status })
            .HasDatabaseName("IX_RedemptionRequests_ProductId_Status");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_RedemptionRequests_Status");
    }
}

