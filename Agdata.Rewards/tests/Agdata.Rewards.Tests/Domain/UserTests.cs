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
        var user = User.CreateNew("Alex Reed", "alex.reed@agdata.com", "AGD-001");

        Assert.Equal("Alex Reed", user.Name);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.TotalPoints);
        Assert.Equal(0, user.LockedPoints);
        Assert.Equal(user.TotalPoints - user.LockedPoints, user.AvailablePoints);
        Assert.Equal("alex.reed@agdata.com", user.Email.Value);
        Assert.Equal("AGD-001", user.EmployeeId.Value);
    }

    [Fact]
    public void AddPoints_ShouldIncreaseTotalPoints()
    {
        var user = User.CreateNew("Brooke Chen", "brooke.chen@agdata.com", "AGD-100");
        user.AddPoints(250);

        Assert.Equal(250, user.TotalPoints);
        Assert.Equal(250, user.AvailablePoints);
    }

    [Fact]
    public void AddPoints_WhenNonPositive_ShouldThrow()
    {
        var user = User.CreateNew("Chad Patel", "chad.patel@agdata.com", "AGD-200");

        Assert.Throws<DomainException>(() => user.AddPoints(0));
        Assert.Throws<DomainException>(() => user.AddPoints(-5));
    }

    [Fact]
    public void LockPoints_WhenInsufficient_ShouldThrow()
    {
        var user = User.CreateNew("Dana Li", "dana.li@agdata.com", "AGD-300");
        user.AddPoints(100);

        Assert.Throws<DomainException>(() => user.LockPoints(200));
    }

    [Fact]
    public void CommitLockedPoints_ShouldReduceTotals()
    {
        var user = User.CreateNew("Evan Ross", "evan.ross@agdata.com", "AGD-400");
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
        var user = User.CreateNew("Fiona Grey", "fiona.grey@agdata.com", "AGD-500");

        user.UpdateName("  New Name  ");

        Assert.Equal("New Name", user.Name);
    }

    [Fact]
    public void Constructor_WithInvalidArguments_ShouldThrow()
    {
        var email = new Email("testing@agdata.com");
        var employeeId = new EmployeeId("AGD-900");

        Assert.Throws<DomainException>(() => new User(Guid.Empty, "Test", email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), " ", email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), "Test", email, employeeId, totalPoints: -1));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), "Test", email, employeeId, totalPoints: 10, lockedPoints: 20));
    }

    [Fact]
    public void LockPoints_WhenAmountNotPositive_ShouldThrow()
    {
        var user = User.CreateNew("Gabe Nolan", "gabe.nolan@agdata.com", "AGD-600");
        user.AddPoints(100);

        Assert.Throws<DomainException>(() => user.LockPoints(0));
        Assert.Throws<DomainException>(() => user.LockPoints(-25));
    }

    [Fact]
    public void UnlockPoints_ShouldReleaseLockedBalance()
    {
        var user = User.CreateNew("Harper Singh", "harper.singh@agdata.com", "AGD-610");
        user.AddPoints(400);
        user.LockPoints(250);

        user.UnlockPoints(150);

        Assert.Equal(400, user.TotalPoints);
        Assert.Equal(100, user.LockedPoints);
        Assert.Equal(300, user.AvailablePoints);
    }

    [Fact]
    public void UnlockPoints_WhenExceedingLocked_ShouldThrow()
    {
        var user = User.CreateNew("Isla Brooks", "isla.brooks@agdata.com", "AGD-620");
        user.AddPoints(150);
        user.LockPoints(100);

        Assert.Throws<DomainException>(() => user.UnlockPoints(0));
        Assert.Throws<DomainException>(() => user.UnlockPoints(-10));
        Assert.Throws<DomainException>(() => user.UnlockPoints(150));
    }

    [Fact]
    public void CommitLockedPoints_WhenExceedingLocked_ShouldThrow()
    {
        var user = User.CreateNew("Jonah Patel", "jonah.patel@agdata.com", "AGD-630");
        user.AddPoints(180);
        user.LockPoints(80);

        Assert.Throws<DomainException>(() => user.CommitLockedPoints(0));
        Assert.Throws<DomainException>(() => user.CommitLockedPoints(-5));
        Assert.Throws<DomainException>(() => user.CommitLockedPoints(100));
    }

    [Fact]
    public void AccountActivation_ShouldToggleStates()
    {
        var user = User.CreateNew("Kara Mills", "kara.mills@agdata.com", "AGD-640");

        user.DeactivateAccount();
        Assert.False(user.IsActive);

        user.ActivateAccount();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void AddPoints_WhenOverflowing_ShouldThrow()
    {
        var email = new Email("overflow@agdata.com");
        var employeeId = new EmployeeId("AGD-650");
        var user = new User(Guid.NewGuid(), "Overflow Check", email, employeeId, totalPoints: int.MaxValue);

        Assert.Throws<OverflowException>(() => user.AddPoints(1));
    }
}
