using System;
using System.Threading.Tasks;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Enums;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Agdata.Rewards.Tests.Infrastructure;

public class SqlServerIntegrationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly string _connectionString;

    public SqlServerIntegrationTests()
    {
        // Load configuration similar to DesignTimeDbContextFactory
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "src", "Agdata.Rewards.Presentation.Api");
        if (!Directory.Exists(basePath))
        {
            basePath = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _connectionString = configuration.GetConnectionString("RewardsDb")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__RewardsDb")
            ?? "Server=FRIDAY\\SQLEXPRESS;Database=RewardsDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(_connectionString);
        _context = new AppDbContext(optionsBuilder.Options);
    }

    [Fact]
    public async Task Database_CanConnect()
    {
        // Arrange & Act
        var canConnect = await _context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect, "Should be able to connect to SQL Server database");
    }

    [Fact]
    public async Task CreateUser_ShouldPersistToDatabase()
    {
        // Arrange - use unique email/employeeId to avoid conflicts
        var uniqueNum = Random.Shared.Next(100000, 999999); // 6-digit number for employee ID
        var user = User.CreateNew("Test", "Integration", "User", $"integration.test.{uniqueNum}@example.com", $"TST-{uniqueNum}");

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(user.Email.Value, retrieved!.Email.Value);
        Assert.Equal(user.EmployeeId.Value, retrieved.EmployeeId.Value);
        Assert.Equal("Test Integration User", retrieved.Name.FullName);
        Assert.NotEmpty(retrieved.RowVersion);

        // Cleanup
        var userToRemove = await _context.Users.FindAsync(user.Id);
        if (userToRemove != null)
        {
            _context.Users.Remove(userToRemove);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ShouldThrow()
    {
        // Arrange - use unique email to avoid conflicts with other tests
        var uniqueNum = Random.Shared.Next(100000, 999999); // 6-digit number for employee ID
        var user1 = User.CreateNew("First", null, "User", $"duplicate.{uniqueNum}@test.com", $"TST-{uniqueNum}1");
        var user2 = User.CreateNew("Second", null, "User", $"duplicate.{uniqueNum}@test.com", $"TST-{uniqueNum}2");

        _context.Users.Add(user1);
        await _context.SaveChangesAsync();

        // Act & Assert
        _context.Users.Add(user2);
        await Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());

        // Cleanup
        var userToRemove = await _context.Users.FindAsync(user1.Id);
        if (userToRemove != null)
        {
            _context.Users.Remove(userToRemove);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task UpdateUser_ShouldUpdateRowVersion()
    {
        // Arrange - use unique email to avoid conflicts
        var uniqueNum = Random.Shared.Next(100000, 999999); // 6-digit number for employee ID
        var user = User.CreateNew("Original", null, "Name", $"rowversion.{uniqueNum}@example.com", $"TST-{uniqueNum}");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var originalRowVersion = user.RowVersion;

        // Act
        user.Rename(PersonName.Create("Updated", null, "Name"));
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(originalRowVersion, user.RowVersion);
        var retrieved = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.Equal("Updated Name", retrieved!.Name.FullName);

        // Cleanup
        var userToRemove = await _context.Users.FindAsync(user.Id);
        if (userToRemove != null)
        {
            _context.Users.Remove(userToRemove);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateProduct_ShouldPersistWithRowVersion()
    {
        // Arrange
        var product = Product.CreateNew("Test Product", 100, stock: 5, description: "Test description");

        // Act
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(product.Name, retrieved!.Name);
        Assert.Equal(product.PointsCost, retrieved.PointsCost);
        Assert.Equal(product.Stock, retrieved.Stock);
        Assert.NotEmpty(retrieved.RowVersion);

        // Cleanup
        var productToRemove = await _context.Products.FindAsync(product.Id);
        if (productToRemove != null)
        {
            _context.Products.Remove(productToRemove);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateEvent_ShouldPersist()
    {
        // Arrange
        var eventEntity = Event.CreateNew("Test Event", DateTimeOffset.UtcNow.AddDays(7));

        // Act
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventEntity.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(eventEntity.Name, retrieved!.Name);
        Assert.Equal(eventEntity.OccursAt, retrieved.OccursAt);
        Assert.True(retrieved.IsActive);

        // Cleanup
        var eventToRemove = await _context.Events.FindAsync(eventEntity.Id);
        if (eventToRemove != null)
        {
            _context.Events.Remove(eventToRemove);
            await _context.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateRedemptionRequest_ShouldPersist()
    {
        // Arrange
        var user = User.CreateNew("Redemption", null, "User", "redemption.test@example.com", "TST-995");
        var product = Product.CreateNew("Redeemable Product", 50);
        _context.Users.Add(user);
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var redemptionRequest = RedemptionRequest.CreateNew(user.Id, product.Id);

        // Act
        _context.RedemptionRequests.Add(redemptionRequest);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.RedemptionRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == redemptionRequest.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved!.UserId);
        Assert.Equal(product.Id, retrieved.ProductId);
        Assert.Equal(RedemptionRequestStatus.Pending, retrieved.Status);

        // Cleanup
        var requestToRemove = await _context.RedemptionRequests.FindAsync(redemptionRequest.Id);
        var userToRemove = await _context.Users.FindAsync(user.Id);
        var productToRemove = await _context.Products.FindAsync(product.Id);
        
        if (requestToRemove != null) _context.RedemptionRequests.Remove(requestToRemove);
        if (userToRemove != null) _context.Users.Remove(userToRemove);
        if (productToRemove != null) _context.Products.Remove(productToRemove);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateLedgerEntry_ShouldPersist()
    {
        // Arrange
        var user = User.CreateNew("Ledger", null, "User", "ledger.test@example.com", "TST-994");
        var eventEntity = Event.CreateNew("Ledger Event", DateTimeOffset.UtcNow);
        _context.Users.Add(user);
        _context.Events.Add(eventEntity);
        await _context.SaveChangesAsync();

        var ledgerEntry = new LedgerEntry(
            Guid.NewGuid(),
            user.Id,
            LedgerEntryType.Earn,
            100,
            DateTimeOffset.UtcNow,
            eventId: eventEntity.Id);

        // Act
        _context.LedgerEntries.Add(ledgerEntry);
        await _context.SaveChangesAsync();

        // Assert
        var retrieved = await _context.LedgerEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == ledgerEntry.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved!.UserId);
        Assert.Equal(eventEntity.Id, retrieved.EventId);
        Assert.Equal(100, retrieved.Points);
        Assert.Equal(LedgerEntryType.Earn, retrieved.Type);

        // Cleanup
        var entryToRemove = await _context.LedgerEntries.FindAsync(ledgerEntry.Id);
        var userToRemove = await _context.Users.FindAsync(user.Id);
        var eventToRemove = await _context.Events.FindAsync(eventEntity.Id);
        
        if (entryToRemove != null) _context.LedgerEntries.Remove(entryToRemove);
        if (userToRemove != null) _context.Users.Remove(userToRemove);
        if (eventToRemove != null) _context.Events.Remove(eventToRemove);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

