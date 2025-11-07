using System;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Agdata.Rewards.Infrastructure.SqlServer.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20251103200935_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.Event", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<DateTimeOffset>("OccursAt")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("OccursAt")
                        .IsDescending()
                        .HasDatabaseName("IX_Events_OccursAt");

                    b.HasIndex("IsActive", "OccursAt")
                        .IsDescending(false, true)
                        .HasDatabaseName("IX_Events_IsActive_OccursAt");

                    b.ToTable("Events", (string)null);
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.LedgerEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("EventId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<int>("Points")
                        .HasColumnType("int");

                    b.Property<Guid?>("RedemptionRequestId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("EventId")
                        .HasDatabaseName("IX_LedgerEntries_EventId");

                    b.HasIndex("RedemptionRequestId")
                        .HasDatabaseName("IX_LedgerEntries_RedemptionRequestId");

                    b.HasIndex("UserId", "Timestamp", "Id")
                        .IsDescending(false, true, false)
                        .HasDatabaseName("IX_LedgerEntries_UserId_Timestamp_Id");

                    b.ToTable("LedgerEntries", null, t =>
                        {
                            t.HasCheckConstraint("CK_LedgerEntries_Points", "[Points] > 0");
                        });
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.Product", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Description")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("ImageUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("nvarchar(200)");

                    b.Property<int>("PointsCost")
                        .HasColumnType("int");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<int?>("Stock")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("IsActive")
                        .HasDatabaseName("IX_Products_IsActive");

                    b.ToTable("Products", null, t =>
                        {
                            t.HasCheckConstraint("CK_Products_PointsCost", "[PointsCost] > 0");

                            t.HasCheckConstraint("CK_Products_Stock", "[Stock] IS NULL OR [Stock] >= 0");
                        });
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.RedemptionRequest", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset?>("ApprovedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("DeliveredAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<Guid>("ProductId")
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("RequestedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("Status")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<Guid>("UserId")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("Id");

                    b.HasIndex("Status")
                        .HasDatabaseName("IX_RedemptionRequests_Status");

                    b.HasIndex("ProductId", "Status")
                        .HasDatabaseName("IX_RedemptionRequests_ProductId_Status");

                    b.HasIndex("UserId", "ProductId", "Status")
                        .HasDatabaseName("IX_RedemptionRequests_UserId_ProductId_Status");

                    b.ToTable("RedemptionRequests", (string)null);
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uniqueidentifier");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bit")
                        .HasDefaultValue(true);

                    b.Property<int>("LockedPoints")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .IsRequired()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("rowversion");

                    b.Property<int>("TotalPoints")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("UserType")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("nvarchar(5)");

                    b.HasKey("Id");

                    b.HasIndex("IsActive")
                        .HasDatabaseName("IX_Users_IsActive");

                    b.ToTable("Users", null, t =>
                        {
                            t.HasCheckConstraint("CK_Users_PointsState", "[TotalPoints] >= 0 AND [LockedPoints] >= 0 AND [TotalPoints] >= [LockedPoints]");
                        });

                    b.HasDiscriminator<string>("UserType").HasValue("User");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.Admin", b =>
                {
                    b.HasBaseType("Agdata.Rewards.Domain.Entities.User");

                    b.ToTable(t =>
                        {
                            t.HasCheckConstraint("CK_Users_PointsState", "[TotalPoints] >= 0 AND [LockedPoints] >= 0 AND [TotalPoints] >= [LockedPoints]");
                        });

                    b.HasDiscriminator().HasValue("Admin");
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.LedgerEntry", b =>
                {
                    b.HasOne("Agdata.Rewards.Domain.Entities.Event", null)
                        .WithMany()
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_LedgerEntries_Events");

                    b.HasOne("Agdata.Rewards.Domain.Entities.RedemptionRequest", null)
                        .WithMany()
                        .HasForeignKey("RedemptionRequestId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .HasConstraintName("FK_LedgerEntries_RedemptionRequests");

                    b.HasOne("Agdata.Rewards.Domain.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_LedgerEntries_Users");
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.RedemptionRequest", b =>
                {
                    b.HasOne("Agdata.Rewards.Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_RedemptionRequests_Products");

                    b.HasOne("Agdata.Rewards.Domain.Entities.User", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired()
                        .HasConstraintName("FK_RedemptionRequests_Users");
                });

            modelBuilder.Entity("Agdata.Rewards.Domain.Entities.User", b =>
                {
                    b.OwnsOne("Agdata.Rewards.Domain.ValueObjects.Email", "Email", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(255)
                                .HasColumnType("nvarchar(255)")
                                .HasColumnName("Email_Value");

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.OwnsOne("Agdata.Rewards.Domain.ValueObjects.EmployeeId", "EmployeeId", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Value")
                                .IsRequired()
                                .HasMaxLength(20)
                                .HasColumnType("nvarchar(20)")
                                .HasColumnName("EmployeeId_Value");

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.OwnsOne("Agdata.Rewards.Domain.ValueObjects.PersonName", "Name", b1 =>
                        {
                            b1.Property<Guid>("UserId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("FirstName")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)")
                                .HasColumnName("Name_FirstName");

                            b1.Property<string>("LastName")
                                .IsRequired()
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)")
                                .HasColumnName("Name_LastName");

                            b1.Property<string>("MiddleName")
                                .HasMaxLength(100)
                                .HasColumnType("nvarchar(100)")
                                .HasColumnName("Name_MiddleName");

                            b1.HasKey("UserId");

                            b1.ToTable("Users");

                            b1.WithOwner()
                                .HasForeignKey("UserId");
                        });

                    b.Navigation("Email")
                        .IsRequired();

                    b.Navigation("EmployeeId")
                        .IsRequired();

                    b.Navigation("Name")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
