using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateNew_ShouldInitialiseUserWithDefaults()
    {
        var user = User.CreateNew("Asha", "asha@example.com", "EMP-001");

        Assert.Equal("Asha", user.Name);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.TotalPoints);
        Assert.Equal(0, user.LockedPoints);
        Assert.Equal(user.TotalPoints - user.LockedPoints, user.AvailablePoints);
        Assert.Equal("asha@example.com", user.Email.Value);
        Assert.Equal("EMP-001", user.EmployeeId.Value);
    }

    [Fact]
    public void AddPoints_ShouldIncreaseTotalPoints()
    {
        var user = User.CreateNew("Mohit", "mohit@example.com", "EMP-100");
        user.AddPoints(250);

        Assert.Equal(250, user.TotalPoints);
        Assert.Equal(250, user.AvailablePoints);
    }

    [Fact]
    public void AddPoints_WhenNonPositive_ShouldThrow()
    {
        var user = User.CreateNew("Ria", "ria@example.com", "EMP-200");

        Assert.Throws<DomainException>(() => user.AddPoints(0));
        Assert.Throws<DomainException>(() => user.AddPoints(-5));
    }

    [Fact]
    public void LockPoints_WhenInsufficient_ShouldThrow()
    {
        var user = User.CreateNew("Ishu", "ishu@example.com", "EMP-300");
        user.AddPoints(100);

        Assert.Throws<DomainException>(() => user.LockPoints(200));
    }

    [Fact]
    public void CommitLockedPoints_ShouldReduceTotals()
    {
        var user = User.CreateNew("Zara", "zara@example.com", "EMP-400");
        user.AddPoints(500);
        user.LockPoints(200);

        user.CommitLockedPoints(200);

        Assert.Equal(300, user.TotalPoints);
        Assert.Equal(0, user.LockedPoints);
        Assert.Equal(300, user.AvailablePoints);
    }

    [Fact]
    public void UpdateName_ShouldTrimAndPersist()
    {
        var user = User.CreateNew("Old", "old@example.com", "EMP-500");

        user.UpdateName("  New Name  ");

        Assert.Equal("New Name", user.Name);
    }

    [Fact]
    public void Constructor_WithInvalidArguments_ShouldThrow()
    {
        var email = new Email("valid@example.com");
        var employeeId = new EmployeeId("EMP-900");

        Assert.Throws<DomainException>(() => new User(Guid.Empty, "Test", email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), " ", email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), "Test", email, employeeId, totalPoints: -1));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), "Test", email, employeeId, totalPoints: 10, lockedPoints: 20));
    }
}
