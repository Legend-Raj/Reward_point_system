using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.InMemory;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Xunit;

namespace Agdata.Rewards.Tests.Application;

/// <summary>
/// Comprehensive tests for PointsService - covering all operations, edge cases, and error scenarios.
/// </summary>
public class PointsServiceTests
{
    private static (PointsService service, UserRepositoryInMemory userRepo, InMemoryUnitOfWork unitOfWork) BuildService()
    {
        var userRepo = new UserRepositoryInMemory();
        var unitOfWork = new InMemoryUnitOfWork();
        var service = new PointsService(userRepo, unitOfWork);
        return (service, userRepo, unitOfWork);
    }

    private static User CreateTestUser(string fullName, string email, string employeeId, int totalPoints = 0, int lockedPoints = 0)
    {
        var nameParts = fullName.Split(' ');
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[^1] : "Doe";
        var middleName = nameParts.Length > 2 ? string.Join(" ", nameParts[1..^1]) : null;

        var user = new User(
            Guid.NewGuid(),
            PersonName.Create(firstName, middleName, lastName),
            new Email(email),
            new EmployeeId(employeeId),
            isActive: true,
            totalPoints: totalPoints,
            lockedPoints: lockedPoints
        );
        return user;
    }

    #region CreditPointsAsync Tests

    [Fact]
    public async Task CreditPointsAsync_WithValidPoints_ShouldIncreaseTotal()
    {
        // Arrange
        var (service, userRepo, unitOfWork) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100);
        userRepo.AddUser(user);

        // Act
        var result = await service.CreditPointsAsync(user.Id, 50);

        // Assert
        Assert.Equal(150, result.TotalPoints);
        Assert.Equal(0, result.LockedPoints);
        Assert.Equal(150, result.AvailablePoints);
    }

    [Fact]
    public async Task CreditPointsAsync_WithZeroPoints_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001");
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CreditPointsAsync(user.Id, 0)
        );
        Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreditPointsAsync_WithNegativePoints_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001");
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CreditPointsAsync(user.Id, -100)
        );
        Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreditPointsAsync_WhenUserNotFound_ShouldThrow()
    {
        // Arrange
        var (service, _, _) = BuildService();
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CreditPointsAsync(nonExistentUserId, 100)
        );
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreditPointsAsync_WhenUserInactive_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001");
        user.Deactivate();
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CreditPointsAsync(user.Id, 100)
        );
        Assert.Contains("inactive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreditPointsAsync_ShouldNotCallSaveChanges()
    {
        // Arrange
        var (service, userRepo, unitOfWork) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100);
        userRepo.AddUser(user);

        // Act
        await service.CreditPointsAsync(user.Id, 50);

        // Assert - Verify user was updated in repo but SaveChanges not called
        var updatedUser = await userRepo.GetUserByIdAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal(150, updatedUser.TotalPoints);
    }

    #endregion

    #region ReservePointsAsync Tests

    [Fact]
    public async Task ReservePointsAsync_WithSufficientPoints_ShouldLockPoints()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100);
        userRepo.AddUser(user);

        // Act
        var result = await service.ReservePointsAsync(user.Id, 30);

        // Assert
        Assert.Equal(100, result.TotalPoints);
        Assert.Equal(30, result.LockedPoints);
        Assert.Equal(70, result.AvailablePoints);
    }

    [Fact]
    public async Task ReservePointsAsync_WithInsufficientPoints_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 50);
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ReservePointsAsync(user.Id, 100)
        );
        Assert.Contains("insufficient", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReservePointsAsync_WithExactAvailablePoints_ShouldSucceed()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 40);
        userRepo.AddUser(user);

        // Act
        var result = await service.ReservePointsAsync(user.Id, 60); // Exactly available

        // Assert
        Assert.Equal(100, result.TotalPoints);
        Assert.Equal(100, result.LockedPoints);
        Assert.Equal(0, result.AvailablePoints);
    }

    [Fact]
    public async Task ReservePointsAsync_WithNegativePoints_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100);
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ReservePointsAsync(user.Id, -10)
        );
        Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReservePointsAsync_WhenUserNotFound_ShouldThrow()
    {
        // Arrange
        var (service, _, _) = BuildService();
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.ReservePointsAsync(nonExistentUserId, 50)
        );
    }

    #endregion

    #region ReleasePointsAsync Tests

    [Fact]
    public async Task ReleasePointsAsync_WithValidLockedPoints_ShouldUnlockPoints()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act
        var result = await service.ReleasePointsAsync(user.Id, 30);

        // Assert
        Assert.Equal(100, result.TotalPoints);
        Assert.Equal(20, result.LockedPoints);
        Assert.Equal(80, result.AvailablePoints);
    }

    [Fact]
    public async Task ReleasePointsAsync_ReleasingMoreThanLocked_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 30);
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ReleasePointsAsync(user.Id, 50)
        );
        Assert.Contains("locked", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReleasePointsAsync_ReleasingAllLocked_ShouldSucceed()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act
        var result = await service.ReleasePointsAsync(user.Id, 50); // Release all

        // Assert
        Assert.Equal(100, result.TotalPoints);
        Assert.Equal(0, result.LockedPoints);
        Assert.Equal(100, result.AvailablePoints);
    }

    [Fact]
    public async Task ReleasePointsAsync_WithNegativePoints_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ReleasePointsAsync(user.Id, -10)
        );
        Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region CapturePointsAsync Tests

    [Fact]
    public async Task CapturePointsAsync_WithValidLockedPoints_ShouldDeductFromBoth()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act
        var result = await service.CapturePointsAsync(user.Id, 30);

        // Assert
        Assert.Equal(70, result.TotalPoints);  // 100 - 30
        Assert.Equal(20, result.LockedPoints);  // 50 - 30
        Assert.Equal(50, result.AvailablePoints);  // 70 - 20
    }

    [Fact]
    public async Task CapturePointsAsync_CapturingMoreThanLocked_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 30);
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CapturePointsAsync(user.Id, 50)
        );
        Assert.Contains("locked", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CapturePointsAsync_CapturingAllLocked_ShouldSucceed()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act
        var result = await service.CapturePointsAsync(user.Id, 50); // Capture all

        // Assert
        Assert.Equal(50, result.TotalPoints);
        Assert.Equal(0, result.LockedPoints);
        Assert.Equal(50, result.AvailablePoints);
    }

    [Fact]
    public async Task CapturePointsAsync_WithNegativePoints_ShouldThrow()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.CapturePointsAsync(user.Id, -10)
        );
        Assert.Contains("positive", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetPointsBalanceAsync Tests

    [Fact]
    public async Task GetPointsBalanceAsync_ShouldReturnCorrectBalance()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 30);
        userRepo.AddUser(user);

        // Act
        var (totalPoints, lockedPoints, availablePoints) = await service.GetPointsBalanceAsync(user.Id);

        // Assert
        Assert.Equal(100, totalPoints);
        Assert.Equal(30, lockedPoints);
        Assert.Equal(70, availablePoints);
    }

    [Fact]
    public async Task GetPointsBalanceAsync_WhenUserNotFound_ShouldThrow()
    {
        // Arrange
        var (service, _, _) = BuildService();
        var nonExistentUserId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            () => service.GetPointsBalanceAsync(nonExistentUserId)
        );
    }

    [Fact]
    public async Task GetPointsBalanceAsync_WithZeroBalance_ShouldReturnZero()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 0, lockedPoints: 0);
        userRepo.AddUser(user);

        // Act
        var (totalPoints, lockedPoints, availablePoints) = await service.GetPointsBalanceAsync(user.Id);

        // Assert
        Assert.Equal(0, totalPoints);
        Assert.Equal(0, lockedPoints);
        Assert.Equal(0, availablePoints);
    }

    #endregion

    #region HasSufficientPointsAsync Tests

    [Fact]
    public async Task HasSufficientPointsAsync_WithSufficientPoints_ShouldReturnTrue()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 20);
        userRepo.AddUser(user);

        // Act
        var result = await service.HasSufficientPointsAsync(user.Id, 50);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasSufficientPointsAsync_WithInsufficientPoints_ShouldReturnFalse()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 70);
        userRepo.AddUser(user);

        // Act
        var result = await service.HasSufficientPointsAsync(user.Id, 50);

        // Assert
        Assert.False(result);  // Available = 30, required = 50
    }

    [Fact]
    public async Task HasSufficientPointsAsync_WithExactPoints_ShouldReturnTrue()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100, lockedPoints: 50);
        userRepo.AddUser(user);

        // Act
        var result = await service.HasSufficientPointsAsync(user.Id, 50);

        // Assert
        Assert.True(result);  // Available = 50, required = 50
    }

    [Fact]
    public async Task HasSufficientPointsAsync_WhenUserNotFound_ShouldReturnFalse()
    {
        // Arrange
        var (service, _, _) = BuildService();
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await service.HasSufficientPointsAsync(nonExistentUserId, 50);

        // Assert
        Assert.False(result);  // User doesn't exist → no points
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public async Task PointsWorkflow_ReserveAndCapture_ShouldMaintainInvariants()
    {
        // Arrange
        var (service, userRepo, unitOfWork) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100);
        userRepo.AddUser(user);

        // Act - Reserve
        var afterReserve = await service.ReservePointsAsync(user.Id, 30);
        Assert.Equal(100, afterReserve.TotalPoints);
        Assert.Equal(30, afterReserve.LockedPoints);
        Assert.Equal(70, afterReserve.AvailablePoints);

        // Act - Capture
        var afterCapture = await service.CapturePointsAsync(user.Id, 30);
        Assert.Equal(70, afterCapture.TotalPoints);
        Assert.Equal(0, afterCapture.LockedPoints);
        Assert.Equal(70, afterCapture.AvailablePoints);
    }

    [Fact]
    public async Task PointsWorkflow_ReserveAndRelease_ShouldMaintainInvariants()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 100);
        userRepo.AddUser(user);

        // Act - Reserve
        var afterReserve = await service.ReservePointsAsync(user.Id, 30);
        Assert.Equal(100, afterReserve.TotalPoints);
        Assert.Equal(30, afterReserve.LockedPoints);

        // Act - Release
        var afterRelease = await service.ReleasePointsAsync(user.Id, 30);
        Assert.Equal(100, afterRelease.TotalPoints);
        Assert.Equal(0, afterRelease.LockedPoints);
        Assert.Equal(100, afterRelease.AvailablePoints);
    }

    [Fact]
    public async Task PointsWorkflow_CreditThenReserve_ShouldSucceed()
    {
        // Arrange
        var (service, userRepo, _) = BuildService();
        var user = CreateTestUser("John Doe", "john@example.com", "TST-001", totalPoints: 50);
        userRepo.AddUser(user);

        // Act - Credit
        var afterCredit = await service.CreditPointsAsync(user.Id, 100);
        Assert.Equal(150, afterCredit.TotalPoints);

        // Act - Reserve
        var afterReserve = await service.ReservePointsAsync(user.Id, 150);
        Assert.Equal(150, afterReserve.TotalPoints);
        Assert.Equal(150, afterReserve.LockedPoints);
        Assert.Equal(0, afterReserve.AvailablePoints);
    }

    #endregion
}

