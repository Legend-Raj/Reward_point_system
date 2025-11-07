using Agdata.Rewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agdata.Rewards.Infrastructure.SqlServer.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.PointsCost)
            .IsRequired();

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Stock);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Products_IsActive");

        builder.ToTable(t => t.HasCheckConstraint("CK_Products_PointsCost", "[PointsCost] > 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Products_Stock", "[Stock] IS NULL OR [Stock] >= 0"));
    }
}

