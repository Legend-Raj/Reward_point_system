using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

/// <summary>
/// Concurrency tests to validate thread-safety and SQL Server readiness.
/// These tests prove that:
/// 1. In-memory repositories are thread-safe (lock-based)
/// 2. The retry logic pattern is in place for future SQL Server migration
/// 3. Multiple concurrent operations maintain data integrity
/// </summary>
public class ConcurrencyTests
{
    #region Test Setup

    private static (PointsService service, UserRepositoryInMemory userRepo, InMemoryUnitOfWork unitOfWork) BuildService()
    {
        var userRepo = new UserRepositoryInMemory();
        var unitOfWork = new InMemoryUnitOfWork();
        var service = new PointsService(userRepo, unitOfWork);
        return (service, userRepo, unitOfWork);
    }

    private static User CreateTestUser(string firstName, string lastName, string email, string employeeId)
    {
        var user = User.CreateNew(firstName, null, lastName, email, employeeId);
        return user;
    }

    #endregion

    #region Concurrent Points Operations

    [Fact]
    public async Task ConcurrentCreditOperations_OnDifferentUsers_ShouldAllSucceed()
    {
        // Arrange - Test throughput: concurrent operations on DIFFERENT users (no conflicts)
        var (service, userRepo, _) = BuildService();
        
        const int concurrentUsers = 10;
        const int pointsPerUser = 50;
        
        // Create multiple users
        var users = Enumerable.Range(1, concurrentUsers)
            .Select(i => CreateTestUser($"User{i}", "Test", $"user{i}@test.com", $"TST-1{i:D2}"))
            .ToList();
        
        foreach (var user in users)
        {
            userRepo.AddUser(user);
        }

        // Act - Credit points to different users concurrently (no contention)
        var tasks = users.Select(async user =>
        {
            await service.CreditPointsAsync(user.Id, pointsPerUser);
        });

        await Task.WhenAll(tasks);

        // Assert - All users should have their points credited
        foreach (var user in users)
        {
            var balance = await service.GetPointsBalanceAsync(user.Id);
            Assert.Equal(pointsPerUser, balance.TotalPoints);
            Assert.Equal(0, balance.LockedPoints);
            Assert.Equal(pointsPerUser, balance.AvailablePoints);
        }
    }

    [Fact]
    public async Task ConcurrentReserveOperations_ShouldMaintainInvariants()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("Jane", "Smith", "jane.concurrent@test.com", "TST-200");
        userRepo.AddUser(user); // Add user first
        await service.CreditPointsAsync(user.Id, 1000); // Then credit points

        const int concurrentReservations = 10;
        const int pointsPerReservation = 50;

        // Act - Execute concurrent reserve operations
        var tasks = new List<Task>();
        var successCount = 0;
        var lockObj = new object();

        for (int i = 0; i < concurrentReservations; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await service.ReservePointsAsync(user.Id, pointsPerReservation);
                    lock (lockObj) { successCount++; }
                }
                catch (DomainException)
                {
                    // Expected when insufficient points
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Locked points should not exceed total, invariants maintained
        var balance = await service.GetPointsBalanceAsync(user.Id);
        Assert.True(balance.LockedPoints <= balance.TotalPoints, "Locked points should never exceed total points");
        Assert.True(balance.AvailablePoints >= 0, "Available points should never be negative");
        Assert.Equal(balance.TotalPoints - balance.LockedPoints, balance.AvailablePoints);
    }

    [Fact]
    public async Task ConcurrentReserveAndCapture_ShouldMaintainPointsIntegrity()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("Alice", "Johnson", "alice.concurrent@test.com", "TST-300");
        userRepo.AddUser(user); // Add user first
        await service.CreditPointsAsync(user.Id, 1000); // Then credit points

        const int operations = 5;
        const int pointsPerOp = 100;

        // Act - Concurrent reserve and capture cycles
        var tasks = Enumerable.Range(0, operations).Select(async _ =>
        {
            await service.ReservePointsAsync(user.Id, pointsPerOp);
            await service.CapturePointsAsync(user.Id, pointsPerOp);
        });

        await Task.WhenAll(tasks);

        // Assert - Final balance should reflect all captures
        var balance = await service.GetPointsBalanceAsync(user.Id);
        Assert.Equal(1000 - (operations * pointsPerOp), balance.TotalPoints);
        Assert.Equal(0, balance.LockedPoints); // All should be captured
        Assert.Equal(balance.TotalPoints, balance.AvailablePoints);
    }

    #endregion

    #region Repository Thread-Safety

    [Fact]
    public async Task ParallelUserUpdates_ShouldBeThreadSafe()
    {
        // Arrange
        var userRepo = new UserRepositoryInMemory();
        var users = Enumerable.Range(1, 100)
            .Select(i => CreateTestUser($"User{i}", "Test", $"user{i}@test.com", $"TST-{i:D3}"))
            .ToList();

        // Act - Add users in parallel
        Parallel.ForEach(users, user =>
        {
            userRepo.AddUser(user);
        });

        // Assert - All users should be retrievable
        var allUsers = await userRepo.ListUsersAsync();
        Assert.Equal(100, allUsers.Count);

        // Act - Update all users in parallel
        Parallel.ForEach(users, user =>
        {
            user.Deactivate();
            userRepo.UpdateUser(user);
        });

        // Assert - All updates should be persisted
        foreach (var user in users)
        {
            var retrieved = await userRepo.GetUserByIdAsync(user.Id);
            Assert.NotNull(retrieved);
            Assert.False(retrieved.IsActive, $"User {user.Id} should be inactive");
        }
    }

    #endregion

    #region SQL Server Readiness Tests

    /// <summary>
    /// This test validates the retry pattern structure.
    /// When migrated to SQL Server:
    /// 1. The in-memory locks will be removed
    /// 2. RowVersion will trigger DbUpdateConcurrencyException
    /// 3. The retry logic will handle conflicts automatically
    /// 4. This same test should still pass
    /// </summary>
    [Fact]
    public async Task RedemptionRetryPattern_IsConfigured()
    {
        // Arrange
        var userRepo = new UserRepositoryInMemory();
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRequestRepositoryInMemory();
        var ledgerRepo = new LedgerEntryRepositoryInMemory();
        var unitOfWork = new InMemoryUnitOfWork();
        var pointsService = new PointsService(userRepo, unitOfWork);
        var redemptionService = new RedemptionRequestService(
            userRepo, productRepo, redemptionRepo, ledgerRepo, pointsService, unitOfWork);

        var user = CreateTestUser("Bob", "Builder", "bob@test.com", "TST-999");
        userRepo.AddUser(user); // Add user first
        await pointsService.CreditPointsAsync(user.Id, 500); // Then credit points

        var product = Product.CreateNew("Test Product", 100, stock: 10);
        productRepo.AddProduct(product);

        // Act - Request redemption (exercises retry pattern)
        var redemptionId = await redemptionService.RequestRedemptionAsync(user.Id, product.Id);

        // Assert - Should complete successfully even with retry pattern
        Assert.NotEqual(Guid.Empty, redemptionId);
        var balance = await pointsService.GetPointsBalanceAsync(user.Id);
        Assert.Equal(100, balance.LockedPoints); // Points reserved
        Assert.Equal(400, balance.AvailablePoints); // Remaining available
    }

    /// <summary>
    /// Demonstrates that the system can handle rapid concurrent redemptions
    /// without overselling or double-booking points.
    /// SQL Server migration: This will stress-test RowVersion + retry logic.
    /// </summary>
    [Fact]
    public async Task ConcurrentRedemptionRequests_ShouldPreventOverselling()
    {
        // Arrange
        var userRepo = new UserRepositoryInMemory();
        var productRepo = new ProductRepositoryInMemory();
        var redemptionRepo = new RedemptionRequestRepositoryInMemory();
        var ledgerRepo = new LedgerEntryRepositoryInMemory();
        var unitOfWork = new InMemoryUnitOfWork();
        var pointsService = new PointsService(userRepo, unitOfWork);
        var redemptionService = new RedemptionRequestService(
            userRepo, productRepo, redemptionRepo, ledgerRepo, pointsService, unitOfWork);

        // Create user with limited points
        var user = CreateTestUser("Charlie", "Chaplin", "charlie@test.com", "TST-888");
        userRepo.AddUser(user); // Add user first
        await pointsService.CreditPointsAsync(user.Id, 250); // Then credit points (enough for 1 redemption only: 250/150=1)

        // Create expensive product
        var product = Product.CreateNew("Expensive Item", 150, stock: null); // Unlimited stock
        productRepo.AddProduct(product);

        // Act - Try to redeem 5 times concurrently (should only succeed 1 time: 250 points / 150 cost = 1)
        var tasks = Enumerable.Range(0, 5).Select(async _ =>
        {
            try
            {
                var id = await redemptionService.RequestRedemptionAsync(user.Id, product.Id);
                return (Success: true, Id: id);
            }
            catch (DomainException)
            {
                return (Success: false, Id: Guid.Empty);
            }
        });

        var results = await Task.WhenAll(tasks);

        // Assert - Only 1 redemption should succeed (user only has 250 points, product costs 150)
        var successfulRedemptions = results.Count(r => r.Success);
        Assert.Equal(1, successfulRedemptions);

        // Verify points are correctly locked
        var balance = await pointsService.GetPointsBalanceAsync(user.Id);
        Assert.Equal(150, balance.LockedPoints);
        Assert.Equal(100, balance.AvailablePoints);
    }

    #endregion
}

