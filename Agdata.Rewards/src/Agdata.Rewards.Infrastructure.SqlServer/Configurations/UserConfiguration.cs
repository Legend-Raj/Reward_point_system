using Agdata.Rewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Agdata.Rewards.Infrastructure.SqlServer.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.HasDiscriminator<string>("UserType")
            .HasValue<User>("User")
            .HasValue<Admin>("Admin");

        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email_Value")
                .HasMaxLength(255)
                .IsRequired();
        });

        builder.OwnsOne(u => u.EmployeeId, empId =>
        {
            empId.Property(e => e.Value)
                .HasColumnName("EmployeeId_Value")
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.OwnsOne(u => u.Name, name =>
        {
            name.Property(n => n.FirstName)
                .HasColumnName("Name_FirstName")
                .HasMaxLength(100)
                .IsRequired();
            
            name.Property(n => n.MiddleName)
                .HasColumnName("Name_MiddleName")
                .HasMaxLength(100);
            
            name.Property(n => n.LastName)
                .HasColumnName("Name_LastName")
                .HasMaxLength(100)
                .IsRequired();
        });

        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.TotalPoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockedPoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.ToTable(t => t.HasCheckConstraint("CK_Users_PointsState", 
            "[TotalPoints] >= 0 AND [LockedPoints] >= 0 AND [TotalPoints] >= [LockedPoints]"));
    }
}

