using System;
using System.Threading;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Tests.Common;
using Xunit;

namespace Agdata.Rewards.Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateNew_ShouldInitialiseUserWithDefaults()
    {
        var parts = NameTestHelper.Split("Alex Reed");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "alex.reed@agdata.com", "AGD-001");

        Assert.Equal("Alex", user.Name.FirstName);
        Assert.Equal("Reed", user.Name.LastName);
        Assert.Equal("Alex Reed", user.Name.FullName);
        Assert.True(user.IsActive);
        Assert.Equal(0, user.TotalPoints);
        Assert.Equal(0, user.LockedPoints);
        Assert.Equal(user.TotalPoints - user.LockedPoints, user.AvailablePoints);
        Assert.Equal("alex.reed@agdata.com", user.Email.Value);
        Assert.Equal("AGD-001", user.EmployeeId.Value);
        Assert.True(user.CreatedAt <= user.UpdatedAt);
        Assert.True((DateTimeOffset.UtcNow - user.CreatedAt) < TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CreditPoints_ShouldIncreaseTotals()
    {
        var parts = NameTestHelper.Split("Brooke Chen");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "brooke.chen@agdata.com", "AGD-100");

        user.CreditPoints(250);

        Assert.Equal(250, user.TotalPoints);
        Assert.Equal(250, user.AvailablePoints);
    }

    [Fact]
    public void CreditPoints_WhenNonPositive_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Chad Patel");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "chad.patel@agdata.com", "AGD-200");

        Assert.Throws<DomainException>(() => user.CreditPoints(0));
        Assert.Throws<DomainException>(() => user.CreditPoints(-5));
    }

    [Fact]
    public void ReservePoints_WhenInsufficient_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Dana Li");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "dana.li@agdata.com", "AGD-300");
        user.CreditPoints(100);

        Assert.Throws<DomainException>(() => user.ReservePoints(200));
    }

    [Fact]
    public void CaptureReservedPoints_ShouldReduceTotals()
    {
        var parts = NameTestHelper.Split("Evan Ross");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "evan.ross@agdata.com", "AGD-400");
        user.CreditPoints(500);
        user.ReservePoints(200);

        user.CaptureReservedPoints(200);

        Assert.Equal(300, user.TotalPoints);
        Assert.Equal(0, user.LockedPoints);
        Assert.Equal(300, user.AvailablePoints);
    }

    [Fact]
    public void Rename_ShouldTrimAndPersist()
    {
        var parts = NameTestHelper.Split("Fiona Grey");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "fiona.grey@agdata.com", "AGD-500");
        var updatedName = PersonName.Create("New", null, "Name");

        user.Rename(updatedName);

        Assert.Equal("New", user.Name.FirstName);
        Assert.Equal("Name", user.Name.LastName);
        Assert.Equal("New Name", user.Name.FullName);
    }

    [Fact]
    public void Mutations_ShouldRefreshUpdatedAt()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var initialName = PersonName.Create("Audit", null, "User");
        var user = new User(
            Guid.NewGuid(),
            initialName,
            new Email("audit.user@agdata.com"),
            new EmployeeId("AGD-777"),
            createdAt: createdAt,
            updatedAt: createdAt);

        user.Rename(PersonName.Create("Audit", null, "Update"));

        Assert.True(user.UpdatedAt > createdAt);

        var afterNameUpdate = user.UpdatedAt;

        user.CreditPoints(50);
        Assert.True(user.UpdatedAt > afterNameUpdate);
    }

    [Fact]
    public void Rename_ShouldAdvanceUpdatedAt()
    {
        var parts = NameTestHelper.Split("Logan Pierce");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "logan.pierce@agdata.com", "AGD-710");

        var before = user.UpdatedAt;
        Thread.Sleep(5);
        user.Rename(PersonName.Create("Logan", "A.", "Pierce"));

        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ChangeEmail_ShouldAdvanceUpdatedAt()
    {
        var parts = NameTestHelper.Split("Avery Nolan");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "avery.nolan@agdata.com", "AGD-712");

        var before = user.UpdatedAt;
        Thread.Sleep(5);
        var newEmail = new Email("avery.n@agdata.com");

        user.ChangeEmail(newEmail);

        Assert.Equal("avery.n@agdata.com", user.Email.Value);
        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ChangeEmail_WhenNull_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Jordan Blake");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "jordan.blake@agdata.com", "AGD-713");

        Assert.Throws<DomainException>(() => user.ChangeEmail(null!));
    }

    [Fact]
    public void ChangeEmployeeId_ShouldAdvanceUpdatedAt()
    {
        var parts = NameTestHelper.Split("Lena Ortiz");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "lena.ortiz@agdata.com", "AGD-714");

        var before = user.UpdatedAt;
        Thread.Sleep(5);
        var newId = new EmployeeId("agd-902");

        user.ChangeEmployeeId(newId);

        Assert.Equal("AGD-902", user.EmployeeId.Value);
        Assert.True(user.UpdatedAt > before);
    }

    [Fact]
    public void ChangeEmployeeId_WhenNull_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Morgan Tate");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "morgan.tate@agdata.com", "AGD-715");

        Assert.Throws<DomainException>(() => user.ChangeEmployeeId(null!));
    }

    [Fact]
    public void Constructor_WithInvalidArguments_ShouldThrow()
    {
        var email = new Email("testing@agdata.com");
        var employeeId = new EmployeeId("AGD-900");
        var name = PersonName.Create("Test", null, "User");

        Assert.Throws<DomainException>(() => new User(Guid.Empty, name, email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), null!, email, employeeId));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), name, email, employeeId, totalPoints: -1));
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), name, email, employeeId, totalPoints: 10, lockedPoints: 20));
    }

    [Fact]
    public void ReservePoints_WhenAmountNotPositive_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Gabe Nolan");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "gabe.nolan@agdata.com", "AGD-600");
        user.CreditPoints(100);

        Assert.Throws<DomainException>(() => user.ReservePoints(0));
        Assert.Throws<DomainException>(() => user.ReservePoints(-25));
    }

    [Fact]
    public void ReleaseReservedPoints_ShouldReduceLockedBalance()
    {
        var parts = NameTestHelper.Split("Harper Singh");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "harper.singh@agdata.com", "AGD-610");
        user.CreditPoints(400);
        user.ReservePoints(250);

        user.ReleaseReservedPoints(150);

        Assert.Equal(400, user.TotalPoints);
        Assert.Equal(100, user.LockedPoints);
        Assert.Equal(300, user.AvailablePoints);
    }

    [Fact]
    public void ReleaseReservedPoints_WhenExceedingReserved_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Isla Brooks");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "isla.brooks@agdata.com", "AGD-620");
        user.CreditPoints(150);
        user.ReservePoints(100);

        Assert.Throws<DomainException>(() => user.ReleaseReservedPoints(0));
        Assert.Throws<DomainException>(() => user.ReleaseReservedPoints(-10));
        Assert.Throws<DomainException>(() => user.ReleaseReservedPoints(150));
    }

    [Fact]
    public void CaptureReservedPoints_WhenExceedingReserved_ShouldThrow()
    {
        var parts = NameTestHelper.Split("Jonah Patel");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "jonah.patel@agdata.com", "AGD-630");
        user.CreditPoints(180);
        user.ReservePoints(80);

        Assert.Throws<DomainException>(() => user.CaptureReservedPoints(0));
        Assert.Throws<DomainException>(() => user.CaptureReservedPoints(-5));
        Assert.Throws<DomainException>(() => user.CaptureReservedPoints(100));
    }

    [Fact]
    public void ActivationFlow_ShouldToggleStates()
    {
        var parts = NameTestHelper.Split("Kara Mills");
        var user = User.CreateNew(parts.First, parts.Middle, parts.Last, "kara.mills@agdata.com", "AGD-640");

        user.Deactivate();
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void CreditPoints_WhenOverflowing_ShouldThrow()
    {
        var email = new Email("overflow@agdata.com");
        var employeeId = new EmployeeId("AGD-650");
        var name = PersonName.Create("Overflow", null, "Check");
        var user = new User(Guid.NewGuid(), name, email, employeeId, totalPoints: int.MaxValue);

        Assert.Throws<OverflowException>(() => user.CreditPoints(1));
    }
}
